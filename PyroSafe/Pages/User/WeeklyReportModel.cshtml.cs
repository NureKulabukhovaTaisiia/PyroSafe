using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

namespace PyroSafe.Pages.User
{
    [Authorize]
    public class WeeklyReportModel : PageModel
    {
        private readonly AppDbContext _context;

        public WeeklyReportModel(AppDbContext context)
        {
            _context = context;
        }

        public void OnGet() { }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostGenerateWeeklyReportAsync([FromBody] ReportRequestDto dto)
        {
            try
            {
                if (dto == null || dto.ZoneId <= 0)
                    return BadRequest(new { success = false, message = "Оберіть зону" });

                // Отримуємо користувача з Claims (бо є [Authorize])
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ??
                                  User.FindFirst("sub");

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                    return BadRequest(new { success = false, message = "Не вдалося визначити користувача" });

                var user = await _context.Users.FindAsync(userId);
                if (user == null || string.IsNullOrWhiteSpace(user.Email))
                    return BadRequest(new { success = false, message = "Користувач не знайдений або немає email" });

                var zone = await _context.Zones.FindAsync(dto.ZoneId);
                if (zone == null)
                    return NotFound(new { success = false, message = "Зона не знайдена" });

                var sensors = await _context.Sensors.Where(s => s.ZoneID == dto.ZoneId).ToListAsync();

                var weekAgo = DateTime.UtcNow.AddDays(-7);
                var events = await _context.Events
                    .Include(e => e.Sensor)
                    .Include(e => e.ResolvedUser)
                    .Where(e => sensors.Select(s => s.ID).Contains(e.SensorID) && e.CreatedAt >= weekAgo)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();

                // === Формуємо текст звіту (тепер ТІЛЬКИ в пам’яті) ===
                var sb = new StringBuilder();
                sb.AppendLine("".PadRight(80, '='));
                sb.AppendLine("               ЗВІТ ПО СИСТЕМІ PYROSAFE");
                sb.AppendLine($"               {weekAgo:dd.MM.yyyy} – {DateTime.Today:dd.MM.yyyy}");
                sb.AppendLine("".PadRight(80, '='));
                sb.AppendLine();
                sb.AppendLine($"Зона: {zone.ZoneName} | Поверх: {zone.Floor} | Площа: {zone.Area} м²");
                sb.AppendLine($"Сенсорів: {sensors.Count} | Івентів за тиждень: {events.Count}");
                sb.AppendLine();
                sb.AppendLine("СЕНСОРИ:");
                foreach (var s in sensors)
                    sb.AppendLine($" • {s.SensorName} ({s.SensorType}) — {s.SensorValue}");

                sb.AppendLine();
                sb.AppendLine("ІВЕНТИ:");
                if (events.Any())
                {
                    foreach (var e in events)
                    {
                        var resolved = e.Status == "Resolved"
                            ? $"Вирішено: {e.ResolvedUser?.Username ?? "—"}"
                            : "НЕ ВИРІШЕНО";
                        sb.AppendLine($" • [{e.CreatedAt:dd.MM HH:mm}] #{e.ID} | {e.Sensor?.SensorName ?? "—"} | {e.Severity} | {resolved}");
                        sb.AppendLine($"   {e.Description}");
                    }
                }
                else sb.AppendLine("   — Немає івентів за тиждень —");

                sb.AppendLine();
                sb.AppendLine("КОМЕНТАР ОХОРОНЦЯ:");
                sb.AppendLine(string.IsNullOrWhiteSpace(dto.Comment) ? "Без коментарів" : dto.Comment);
                sb.AppendLine();
                sb.AppendLine($"Згенеровано: {DateTime.Now:dd.MM.yyyy HH:mm}");
                sb.AppendLine($"Охоронець: {user.Username}");
                sb.AppendLine("".PadRight(80, '='));

                string reportText = sb.ToString();
                byte[] fileBytes = Encoding.UTF8.GetBytes(reportText);

                var safeZoneName = Regex.Replace(zone.ZoneName ?? "Unknown", @"[^a-zA-Z0-9_-]", "_");
                var fileName = $"PyroSafe_Звіт_{safeZoneName}_{DateTime.Now:yyyyMMdd_HHmm}.txt";

                // 1. Спочатку запускаємо відправку email у фоні
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var attachmentBytes = fileBytes;

                        using var smtp = new SmtpClient("smtp.gmail.com")
                        {
                            Port = 587,
                            Credentials = new NetworkCredential("pyrosafebot@gmail.com", "nmgg fkwb igcw kqad"),
                            EnableSsl = true,
                        };

                        using var mail = new MailMessage
                        {
                            From = new MailAddress("pyrosafebot@gmail.com", "PyroSafe System"),
                            Subject = $"Звіт PyroSafe — {zone.ZoneName} — {DateTime.Today:dd.MM.yyyy}",
                            Body = $"Вітаю, {user.Username}!\n\nУ вкладенні звіт за останній тиждень по зоні \"{zone.ZoneName}\".\n\nЗ повагою,\nPyroSafe System",
                            IsBodyHtml = false
                        };

                        mail.To.Add(user.Email);

                        using var stream = new MemoryStream(attachmentBytes);
                        mail.Attachments.Add(new Attachment(stream, fileName, "text/plain"));

                        await smtp.SendMailAsync(mail);
                    }
                    catch (Exception ex)
                    {
                        // Логування помилки
                        var logPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logs");
                        Directory.CreateDirectory(logPath); // створюємо папку, якщо немає
                        System.IO.File.AppendAllText(
                            Path.Combine(logPath, "email-errors.txt"),
                            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {user?.Email} | {ex.Message}\n{ex.StackTrace}\n\n");
                    }
                });

                // 2. Потім одразу повертаємо результат клієнту (email іде фоном)
                return new JsonResult(new
                {
                    success = true,
                    message = "Звіт згенеровано!",
                    download = true,
                    fileName = fileName,
                    fileContentBase64 = Convert.ToBase64String(fileBytes),
                    emailSent = false   // ми не чекаємо email, тому завжди false (або можна прибрати)
                });

                // Користувач вже отримав файл, email іде фоном
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Помилка: " + ex.Message });
            }
        }
    }

    public class ReportRequestDto
    {
        public int ZoneId { get; set; }
        public string? Comment { get; set; }
    }
}