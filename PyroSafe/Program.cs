using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;
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

    // важливо для Render
    options.Cookie.SameSite = SameSiteMode.Lax;
});

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
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// ===== Forwarded headers =====
app.UseForwardedHeaders();

// ===== HTTPS redirect =====
app.UseHttpsRedirection();

// ===== Static files (необхідно!) =====
app.UseStaticFiles();

// ===== Routing =====
app.UseRouting();

// ===== CORS =====
app.UseCors("AllowApp");

// ===== Session =====
app.UseSession();

// ===== Authorization =====
app.UseAuthorization();

// ===== Controllers & Pages =====
app.MapControllers();
app.MapRazorPages();

// ===== Swagger =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ===== Auto-open browser (LOCAL ONLY) =====
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
