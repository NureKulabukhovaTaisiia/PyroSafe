using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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
            try
            {
                if (dto == null || dto.ZoneId < 0)
                    return BadRequest(new { success = false, message = "Оберіть зону" });
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ??
                                  User.FindFirst("sub");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                    return BadRequest(new { success = false, message = "Не вдалося визначити користувача" });
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return BadRequest(new { success = false, message = "Користувач не знайдений" });
                // Завантаження зон
                var zones = dto.ZoneId == 0
                    ? await _context.Zones.ToListAsync()
                    : new List<Zone> { await _context.Zones.FindAsync(dto.ZoneId) };
                if (zones.Any(z => z == null))
                    return NotFound(new { success = false, message = "Зона не знайдена" });
                // Сенсори та івенти
                var allSensors = await _context.Sensors
                    .Where(s => zones.Select(z => z.ID).Contains(s.ZoneID))
                    .ToListAsync();
                var weekAgo = DateTime.UtcNow.AddDays(-7);
                var events = await _context.Events
                    .Include(e => e.Sensor)
                    .Include(e => e.ResolvedUser)
                    .Where(e => allSensors.Select(s => s.ID).Contains(e.SensorID) && e.CreatedAt >= weekAgo)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();
                // Формування звіту
                var sb = new StringBuilder();
                sb.AppendLine("".PadRight(80, '='));
                sb.AppendLine(" ЗВІТ ПО СИСТЕМІ PYROSAFE");
                sb.AppendLine($" {weekAgo:dd.MM.yyyy} – {DateTime.Today:dd.MM.yyyy}");
                sb.AppendLine("".PadRight(80, '='));
                sb.AppendLine();
                if (dto.ZoneId == 0)
                {
                    sb.AppendLine($"ВСІ ЗОНИ: {zones.Count} | Загальна площа: {zones.Sum(z => z.Area)} м²");
                    sb.AppendLine($"Сенсорів всього: {allSensors.Count} | Івентів за тиждень: {events.Count}");
                    sb.AppendLine();
                    sb.AppendLine("ДЕТАЛІ ПО ЗОНАХ:");
                    sb.AppendLine();
                }
                foreach (var zone in zones)
                {
                    if (dto.ZoneId != 0)
                    {
                        sb.AppendLine($"Зона: {zone.ZoneName} | Поверх: {zone.Floor} | Площа: {zone.Area} м²");
                        sb.AppendLine($"Сенсорів: {allSensors.Count(s => s.ZoneID == zone.ID)} | Івентів за тиждень: {events.Count(e => e.Sensor?.ZoneID == zone.ID)}");
                        sb.AppendLine();
                    }
                    else
                    {
                        sb.AppendLine($"ЗОНА: {zone.ZoneName} | Поверх: {zone.Floor} | Площа: {zone.Area} м²");
                        sb.AppendLine($"Сенсорів: {allSensors.Count(s => s.ZoneID == zone.ID)} | Івентів за тиждень: {events.Count(e => e.Sensor?.ZoneID == zone.ID)}");
                        sb.AppendLine();
                    }
                    sb.AppendLine("СЕНСОРИ:");
                    var zoneSensors = allSensors.Where(s => s.ZoneID == zone.ID);
                    if (zoneSensors.Any())
                    {
                        foreach (var s in zoneSensors)
                            sb.AppendLine($" • {s.SensorName} ({s.SensorType}) — {s.SensorValue}");
                    }
                    else sb.AppendLine(" — Немає сенсорів —");
                    sb.AppendLine();
                    sb.AppendLine("ІВЕНТИ:");
                    var zoneEvents = events.Where(e => e.Sensor?.ZoneID == zone.ID);
                    if (zoneEvents.Any())
                    {
                        foreach (var e in zoneEvents)
                        {
                            var resolved = e.Status == "Resolved"
                                ? $"Вирішено: {e.ResolvedUser?.Username ?? "—"}"
                                : "НЕ ВИРІШЕНО";
                            sb.AppendLine($" • [{e.CreatedAt:dd.MM HH:mm}] #{e.ID} | {e.Sensor?.SensorName ?? "—"} | {e.Severity} | {resolved}");
                            sb.AppendLine($" {e.Description}");
                        }
                    }
                    else sb.AppendLine(" — Немає івентів за тиждень —");
                    sb.AppendLine();
                }
                sb.AppendLine("КОМЕНТАР ОХОРОНЦЯ:");
                sb.AppendLine(string.IsNullOrWhiteSpace(dto.Comment) ? "Без коментарів" : dto.Comment);
                sb.AppendLine();
                sb.AppendLine($"Згенеровано: {DateTime.Now:dd.MM.yyyy HH:mm}");
                sb.AppendLine($"Охоронець: {user.Username}");
                sb.AppendLine("".PadRight(80, '='));
                string reportText = sb.ToString();
                byte[] fileBytes = Encoding.UTF8.GetBytes(reportText);
                var safeZoneName = dto.ZoneId == 0
                    ? "All_Zones"
                    : Regex.Replace(zones.First().ZoneName ?? "Unknown", @"[^a-zA-Z0-9_-]", "_");
                var fileName = $"PyroSafe_Звіт_{safeZoneName}_{DateTime.Now:yyyyMMdd_HHmm}.txt";
                // Повертаємо файл користувачу
                return new JsonResult(new
                {
                    success = true,
                    message = "Звіт згенеровано!",
                    download = true,
                    fileName,
                    fileContentBase64 = Convert.ToBase64String(fileBytes)
                });
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