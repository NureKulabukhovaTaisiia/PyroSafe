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
        public WeeklyReportModel(AppDbContext context) => _context = context;

        public void OnGet() { }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostGenerateWeeklyReportAsync([FromBody] ReportRequestDto dto)
        {
            var emailLog = new StringBuilder();

            try
            {
                emailLog.AppendLine("🔍 ПОЧАТОК ГЕНЕРАЦІЇ ЗВІТУ");

                if (dto == null || dto.ZoneId <= 0)
                {
                    emailLog.AppendLine("❌ Невалідний ZoneId");
                    return BadRequest(new { success = false, message = "Оберіть зону", emailLog = emailLog.ToString() });
                }

                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ??
                                  User.FindFirst("sub");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    emailLog.AppendLine("❌ Не вдалося отримати userId");
                    return BadRequest(new { success = false, message = "Не вдалося визначити користувача", emailLog = emailLog.ToString() });
                }

                emailLog.AppendLine($"✅ UserId: {userId}");

                var user = await _context.Users.FindAsync(userId);
                if (user == null || string.IsNullOrWhiteSpace(user.Email))
                {
                    emailLog.AppendLine("❌ Користувач або email не знайдено");
                    return BadRequest(new { success = false, message = "Користувач не знайдений або немає email", emailLog = emailLog.ToString() });
                }

                emailLog.AppendLine($"✅ User Email: {user.Email}");

                var zone = await _context.Zones.FindAsync(dto.ZoneId);
                if (zone == null)
                {
                    emailLog.AppendLine("❌ Зона не знайдена");
                    return NotFound(new { success = false, message = "Зона не знайдена", emailLog = emailLog.ToString() });
                }

                emailLog.AppendLine($"✅ Zone: {zone.ZoneName}");

                var sensors = await _context.Sensors.Where(s => s.ZoneID == dto.ZoneId).ToListAsync();
                var weekAgo = DateTime.UtcNow.AddDays(-7);
                var events = await _context.Events
                    .Include(e => e.Sensor)
                    .Include(e => e.ResolvedUser)
                    .Where(e => sensors.Select(s => s.ID).Contains(e.SensorID) && e.CreatedAt >= weekAgo)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();

                emailLog.AppendLine($"✅ Sensors: {sensors.Count}, Events: {events.Count}");

                // Формування звіту
                var sb = new StringBuilder();
                sb.AppendLine("".PadRight(80, '='));
                sb.AppendLine(" ЗВІТ ПО СИСТЕМІ PYROSAFE");
                sb.AppendLine($" {weekAgo:dd.MM.yyyy} – {DateTime.Today:dd.MM.yyyy}");
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
                else sb.AppendLine(" — Немає івентів за тиждень —");

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

                emailLog.AppendLine($"✅ Звіт створено, розмір: {fileBytes.Length} bytes");

                // ВІДПРАВКА EMAIL
                emailLog.AppendLine("📧 СПРОБА ВІДПРАВКИ EMAIL...");

                try
                {
                    emailLog.AppendLine("  → Створення SMTP клієнта...");

                    using var smtp = new SmtpClient("smtp.gmail.com", 587)
                    {
                        Credentials = new NetworkCredential("pyrosafebot@gmail.com", "nmgg fkwb igcw kqad"),
                        EnableSsl = true,
                        Timeout = 10000 // 10 секунд таймаут
                    };

                    emailLog.AppendLine("  → SMTP клієнт створено");
                    emailLog.AppendLine("  → Створення повідомлення...");

                    using var mail = new MailMessage();
                    mail.From = new MailAddress("pyrosafebot@gmail.com", "PyroSafe");
                    mail.To.Add(user.Email);
                    mail.Subject = $"Звіт PyroSafe — {zone.ZoneName}";
                    mail.Body = $"Доброго дня!\n\nВаш тижневий звіт по зоні '{zone.ZoneName}' у вкладенні.\n\n" +
                                $"Період: {weekAgo:dd.MM.yyyy} - {DateTime.Today:dd.MM.yyyy}\n" +
                                $"Сенсорів: {sensors.Count}\n" +
                                $"Івентів: {events.Count}\n\n" +
                                $"З повагою,\nСистема PyroSafe";

                    emailLog.AppendLine($"  → Від: pyrosafebot@gmail.com");
                    emailLog.AppendLine($"  → Кому: {user.Email}");
                    emailLog.AppendLine($"  → Тема: {mail.Subject}");

                    // Створюємо вкладення БЕЗ using
                    var stream = new MemoryStream(fileBytes);
                    var attachment = new Attachment(stream, fileName, "text/plain; charset=utf-8");
                    mail.Attachments.Add(attachment);

                    emailLog.AppendLine($"  → Вкладення: {fileName} ({fileBytes.Length} bytes)");
                    emailLog.AppendLine("  → Відправка...");

                    await smtp.SendMailAsync(mail);

                    emailLog.AppendLine("✅ EMAIL УСПІШНО ВІДПРАВЛЕНО!");

                    // Очищаємо ресурси
                    attachment.Dispose();
                    stream.Dispose();
                }
                catch (SmtpException smtpEx)
                {
                    emailLog.AppendLine($"❌ SMTP ПОМИЛКА:");
                    emailLog.AppendLine($"   Message: {smtpEx.Message}");
                    emailLog.AppendLine($"   StatusCode: {smtpEx.StatusCode}");
                    emailLog.AppendLine($"   InnerException: {smtpEx.InnerException?.Message ?? "null"}");
                }
                catch (Exception ex)
                {
                    emailLog.AppendLine($"❌ EMAIL ПОМИЛКА:");
                    emailLog.AppendLine($"   Type: {ex.GetType().Name}");
                    emailLog.AppendLine($"   Message: {ex.Message}");
                    emailLog.AppendLine($"   StackTrace: {ex.StackTrace}");
                }

                emailLog.AppendLine("🏁 ЗАВЕРШЕННЯ ГЕНЕРАЦІЇ ЗВІТУ");

                // Повертаємо файл користувачу
                return new JsonResult(new
                {
                    success = true,
                    message = "Звіт згенеровано!",
                    download = true,
                    fileName,
                    fileContentBase64 = Convert.ToBase64String(fileBytes),
                    emailLog = emailLog.ToString()
                });
            }
            catch (Exception ex)
            {
                emailLog.AppendLine($"❌ КРИТИЧНА ПОМИЛКА: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Помилка: " + ex.Message,
                    emailLog = emailLog.ToString()
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