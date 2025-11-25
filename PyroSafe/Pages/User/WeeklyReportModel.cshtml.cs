using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http; // ← ОБОВ'ЯЗКОВО! Для GetInt32(), SetInt32() тощо
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

namespace PyroSafe.Pages.User
{
    public class WeeklyReportModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public WeeklyReportModel(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public void OnGet() { }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostGenerateWeeklyReportAsync([FromBody] ReportRequestDto dto)
        {
            try
            {
                // Валідація
                if (dto == null || dto.ZoneId <= 0)
                    return BadRequest(new { message = "Оберіть зону" });

                // Правильне отримання UserId з сесії (.NET 6+)
                int? sessionUserId = HttpContext.Session.GetInt32("UserId");
                if (!sessionUserId.HasValue)
                    return BadRequest(new { message = "Ви не авторизовані. Увійдіть знову." });

                int userId = sessionUserId.Value;

                var user = await _context.Users.FindAsync(userId);
                if (user == null || string.IsNullOrWhiteSpace(user.Email))
                    return BadRequest(new { message = "Користувач не знайдений або немає email" });

                var zone = await _context.Zones.FindAsync(dto.ZoneId);
                if (zone == null)
                    return NotFound(new { message = "Зона не знайдена" });

                // Отримання сенсорів та івентів
                var sensors = await _context.Sensors
                    .Where(s => s.ZoneID == dto.ZoneId)
                    .ToListAsync();

                var weekAgo = DateTime.UtcNow.AddDays(-7);
                var events = await _context.Events
                    .Include(e => e.Sensor)
                    .Include(e => e.ResolvedUser)
                    .Where(e => sensors.Select(s => s.ID).Contains(e.SensorID) && e.CreatedAt >= weekAgo)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();

                // Формування звіту
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
                else
                {
                    sb.AppendLine("   — Немає івентів за тиждень —");
                }

                sb.AppendLine();
                sb.AppendLine("КОМЕНТАР ОХОРОНЦЯ:");
                sb.AppendLine(string.IsNullOrWhiteSpace(dto.Comment) ? "Без коментарів" : dto.Comment);
                sb.AppendLine();
                sb.AppendLine($"Згенеровано: {DateTime.Now:dd.MM.yyyy HH:mm}");
                sb.AppendLine($"Охоронець: {user.Username}");
                sb.AppendLine("".PadRight(80, '='));

                var reportText = sb.ToString();

                // Безпечна назва файлу + тимчасовий шлях (працює на Render.com!)
                var safeZoneName = Regex.Replace(zone.ZoneName ?? "Unknown", @"[^a-zA-Z0-9_-]", "_");
                var fileName = $"PyroSafe_Звіт_{safeZoneName}_{DateTime.Now:yyyyMMdd_HHmm}.txt";
                var tempPath = Path.Combine(Path.GetTempPath(), fileName);

                await System.IO.File.WriteAllTextAsync(tempPath, reportText, Encoding.UTF8);

                // Надсилання email
                try
                {
                    var smtp = new SmtpClient("smtp.gmail.com")
                    {
                        Port = 587,
                        Credentials = new NetworkCredential("pyrosafebot@gmail.com", "sjzv sgyq cako yxgf"),
                        EnableSsl = true,
                    };

                    var mail = new MailMessage
                    {
                        From = new MailAddress("pyrosafebot@gmail.com"),
                        Subject = $"Звіт PyroSafe — {zone.ZoneName} — {DateTime.Today:dd.MM.yyyy}",
                        Body = $"Вітаю, {user.Username}!\n\nУ вкладенні звіт за останній тиждень.\n\nЗ повагою,\nPyroSafe System",
                        IsBodyHtml = false
                    };
                    mail.To.Add(user.Email);
                    mail.Attachments.Add(new Attachment(tempPath));
                    await smtp.SendMailAsync(mail);

                    // Видаляємо файл після відправки
                    try { System.IO.File.Delete(tempPath); } catch { }

                    return new JsonResult(new
                    {
                        success = true,
                        message = "Звіт успішно створено та надіслано на email!",
                        fileName
                    });
                }
                catch (Exception ex)
                {
                    // Якщо email не відправився — пропонуємо скачати (тимчасово)
                    return new JsonResult(new
                    {
                        success = true,
                        message = "Звіт створено! Але не вдалося надіслати email: " + ex.Message,
                        downloadUrl = $"/reports/download?file={Uri.EscapeDataString(fileName)}",
                        fileName,
                        note = "Файл доступний для скачування (тимчасово)"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Помилка сервера: " + ex.Message,
                    details = ex.ToString()
                });
            }
        }
    }

    public class ReportRequestDto
    {
        public int ZoneId { get; set; }
        public string? Comment { get; set; }
    }
}