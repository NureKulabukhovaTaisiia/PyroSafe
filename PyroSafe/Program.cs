using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// ===== Підключення до PostgreSQL =====
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSqlConnection")));

// ===== CORS =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ===== Контролери та Razor Pages =====
builder.Services.AddControllers();
builder.Services.AddRazorPages();

// ===== Сесія =====
builder.Services.AddDistributedMemoryCache(); // Пам'ять для сесій
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1); // Час життя сесії
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


// ===== IHttpContextAccessor =====
builder.Services.AddHttpContextAccessor();

// ===== Swagger =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PyroSafe API",
        Version = "v1",
        Description = "API для системи управління пожежною безпекою"
    });
});

var app = builder.Build();

// ===== CORS =====
app.UseCors("AllowAll");

// ===== Swagger =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PyroSafe API v1");
        c.RoutePrefix = "swagger"; // Swagger буде на /swagger
    });
}

// ===== HTTPS =====
app.UseHttpsRedirection();

// ===== Сесія =====
app.UseSession();

app.UseAuthorization();

// ===== Контролери та Razor Pages =====
app.MapControllers();
app.MapRazorPages();

// ===== Автооткриття браузера з реєстрацією =====
var url = "https://localhost:7080/Account/Register"; // стартова сторінка реєстрації
try
{
    Process.Start(new ProcessStartInfo
    {
        FileName = url,
        UseShellExecute = true
    });
}
catch
{
    Console.WriteLine("Не вдалося автоматично відкрити браузер.");
}

app.Run();
