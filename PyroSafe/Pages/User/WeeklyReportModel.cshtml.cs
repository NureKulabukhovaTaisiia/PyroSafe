using Microsoft.AspNetCore.Mvc;           // ← ЦЕ ОБОВ’ЯЗКОВО ДЛЯ Json() і BadRequest()
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using System.Text;

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

        public void OnGet()
        {
        }

        [HttpPost]
        public async Task<IActionResult> OnPostGenerateWeeklyReportAsync([FromBody] ReportRequestDto dto)
        {
            if (dto == null || dto.ZoneId <= 0)
                return BadRequest(new { message = "Оберіть зону" });

            var userId = HttpContext.Session.GetInt32("UserId") ?? 1;
            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.Email))
                return BadRequest(new { message = "Не вдалося визначити користувача або email" });

            var zone = await _context.Zones.FindAsync(dto.ZoneId);
            if (zone == null)
                return NotFound(new { message = "Зона не знайдена" });

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

            // Формуємо текст звіту
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
                    sb.AppendLine($" • [{e.CreatedAt:dd.MM HH:mm}] #{e.ID} | {e.Sensor.SensorName} | {e.Severity} | {resolved}");
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

            var reportText = sb.ToString();
            var fileName = $"PyroSafe_Звіт_{zone.ZoneName}_{DateTime.Now:yyyyMMdd_HHmm}.txt";

            // Зберігаємо у wwwroot/reports
            var reportsFolder = Path.Combine(_env.WebRootPath, "reports");
            Directory.CreateDirectory(reportsFolder);
            var filePath = Path.Combine(reportsFolder, fileName);
            await System.IO.File.WriteAllTextAsync(filePath, reportText, Encoding.UTF8);

            // Надсилаємо email
            try
            {
                var smtp = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("pyrosafebot@gmail.com", "pwrq hmfl hvws afsl"),
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
                mail.Attachments.Add(new Attachment(filePath));
                await smtp.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                // Навіть якщо email не відправився — файл доступний
                return new JsonResult(new
                {
                    success = true,
                    message = "Звіт створено! Але email не відправився: " + ex.Message,
                    downloadUrl = $"/reports/{fileName}",
                    fileName
                });
            }

            // Успіх — все добре
            return new JsonResult(new
            {
                success = true,
                message = "Звіт створено та надіслано на ваш email!",
                downloadUrl = $"/reports/{fileName}",
                fileName
            });
        }
    }

    public class ReportRequestDto
    {
        public int ZoneId { get; set; }
        public string Comment { get; set; } = "";
    }
}