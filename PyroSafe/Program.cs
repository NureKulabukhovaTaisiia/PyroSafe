using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// ===== BASE DOMAIN =====
var baseDomain = builder.Configuration["BASE_DOMAIN"]
                 ?? "http://localhost:7080";

// ===== PostgreSQL =====
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSqlConnection")));

// ===== CORS =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ===== Controllers & Razor Pages =====
builder.Services.AddControllers();
builder.Services.AddRazorPages();

// ===== Session =====
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// ===== АВТЕНТИФІКАЦІЯ — ТУТ! ДО Build() =====
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization(); // теж тут!

// ===== HttpContextAccessor =====
builder.Services.AddHttpContextAccessor();

// ===== Swagger =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PyroSafe API",
        Version = "v1"
    });
});

// ===== Forwarded Headers (Render) =====
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// ========== ВСЕ, ЩО НИЖЧЕ — ПІСЛЯ Build() ==========
var app = builder.Build();

app.UseForwardedHeaders();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowApp");
app.UseSession();

// ← ПРАВИЛЬНИЙ ПОРЯДОК!
app.UseAuthentication();   // ← Обов’язково після UseSession() і перед UseAuthorization()
app.UseAuthorization();    // ← Після Authentication!

app.MapControllers();
app.MapRazorPages();

// Swagger тільки в деві
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Автовідкриття (локально)
if (app.Environment.IsDevelopment())
{
    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://pyrosafe-o880.onrender.com/Account/Register",
            UseShellExecute = true
        });
    }
    catch { }
}

app.Run();