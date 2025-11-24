using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

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

// ===== Razor + Controllers =====
builder.Services.AddControllers();
builder.Services.AddRazorPages();

// ===== Session FIXED VERSION =====
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".PyroSafe.Session";     // Унікальне імʼя cookie
    options.IdleTimeout = TimeSpan.FromHours(2);   // Час життя сесії
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Render вимагає
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
        Version = "v1"
    });
});

// ===== Forwarded headers (обов'язково для Render) =====
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// ===== Enable forwarded headers =====
app.UseForwardedHeaders();

// ===== CORS =====
app.UseCors("AllowApp");

// ===== Swagger =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PyroSafe API v1");
    });
}

app.UseHttpsRedirection();

// ===== STATIC FILES (важливо для cookie!) =====
app.UseStaticFiles();

// ===== Routing =====
app.UseRouting();

// ===== SESSION (має бути ТУТ перед auth/pages) =====
app.UseSession();

// ===== Authorization =====
app.UseAuthorization();

// ===== Razor / Controllers =====
app.MapRazorPages();
app.MapControllers();

// ===== Auto-open browser (local only) =====
if (app.Environment.IsDevelopment())
{
    var url = "http://localhost:7080/Account/Register";
    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
    catch { }
}

app.Run();
