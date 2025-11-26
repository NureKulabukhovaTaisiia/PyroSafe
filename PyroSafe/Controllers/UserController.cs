using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    // -------------------- CREATE --------------------
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] UserCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("Ім'я та пароль обов'язкові");

        var existing = await _context.Users.AnyAsync(u => u.Email == dto.Email || u.Username == dto.Username);
        if (existing)
            return BadRequest("Користувач з таким email або ім'ям вже існує");

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            Phone = dto.Phone,
            Password = dto.Password, 
            UserRole = dto.UserRole
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Created($"/api/users/{user.ID}", new { user.ID, user.Username });
    }

    [HttpGet("current-email")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUserEmail()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var email = await _context.Users
            .Where(u => u.ID == userId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync();

        return Ok(new { email = email ?? "" });
    }

    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        if (!User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Не авторизований" });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(ClaimTypes.Name)?.Value;

        return Ok(new
        {
            userId,
            username,
            isAuthenticated = true
        });
    }
    // -------------------- READ --------------------
    // Получить всех пользователей
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
    {
        var users = await _context.Users
            .Select(u => new UserDto
            {
                ID = u.ID,
                Username = u.Username,
                Email = u.Email,
                Phone = u.Phone,
                UserRole = u.UserRole
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUserById(int id)
    {
        var user = await _context.Users
            .Where(u => u.ID == id)
            .Select(u => new UserDto
            {
                ID = u.ID,
                Username = u.Username,
                Email = u.Email,
                Phone = u.Phone,
                UserRole = u.UserRole
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound();

        return Ok(user);
    }

    // -------------------- UPDATE --------------------
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        // Обновляем поля
        user.Username = dto.Username ?? user.Username;
        user.Email = dto.Email ?? user.Email;
        user.Phone = dto.Phone ?? user.Phone;
        user.UserRole = dto.UserRole;

        if (!string.IsNullOrEmpty(dto.Password))
            user.Password = dto.Password; 

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // -------------------- DELETE --------------------
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // POST: api/users/login
    // POST: api/users/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
            return BadRequest("Введіть email та пароль");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email && u.Password == dto.Password);

        if (user == null)
            return BadRequest("Невірний email або пароль");

        // Записуємо в сесію (для старих сторінок)
        HttpContext.Session.SetInt32("UserId", user.ID);
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetString("Email", user.Email);

        // Куки + Claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email ?? ""), // важливо!
            new Claim(ClaimTypes.Role, user.UserRole ? "Admin" : "User")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12),
                AllowRefresh = true
            });

        return Ok(new
        {
            success = true,
            message = "Успішний вхід",
            user = new { user.ID, user.Username, user.Email, user.UserRole }
        });
    }
}

// -------------------- DTO --------------------

public class LoginDto
{
    public string Email { get; set; }
    public string Password { get; set; }
}
public class UserCreateDto
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Password { get; set; }
    public bool UserRole { get; set; } = false;
}

public class UserUpdateDto
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Password { get; set; }
    public bool UserRole { get; set; } = false;
}

public class UserDto
{
    public int ID { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public bool UserRole { get; set; }
}
