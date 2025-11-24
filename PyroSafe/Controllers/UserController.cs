using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
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
        if (!string.IsNullOrEmpty(dto.Email))
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (existingUser != null)
                return BadRequest("Пользователь с таким email уже существует.");
        }

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

        return CreatedAtAction(nameof(GetUserById), new { id = user.ID }, user);
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
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email && u.Password == dto.Password);

        if (user == null)
            return BadRequest("Неправильний email або пароль");

        // ===== Збереження в сесії =====
        HttpContext.Session.SetInt32("UserID", user.ID);
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetString("Email", user.Email);
        HttpContext.Session.SetString("UserRole", user.UserRole.ToString());

        return Ok(new { user.ID, user.Username, user.Email, user.UserRole });
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
