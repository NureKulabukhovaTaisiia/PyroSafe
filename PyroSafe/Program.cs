using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// ===== BASE DOMAIN (локально и в проде) =====
var baseDomain = builder.Configuration["BASE_DOMAIN"]
                 ?? "http://localhost:7080";
// На Render это может быть: https://pyrosafe.onrender.com


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

// ===== Forwarded Headers (обовʼязково для Render!) =====
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
        c.RoutePrefix = "swagger";
    });
}

// ===== HTTPS redirection =====
app.UseHttpsRedirection();

// ===== !!! Routing must go here !!! =====
app.UseRouting();

// ===== Session =====
app.UseSession();

app.UseAuthorization();

// ===== Controllers & Pages =====
app.MapControllers();
app.MapRazorPages();

// ===== Auto-open browser (ONLY LOCAL) =====
if (app.Environment.IsDevelopment())
{
    var url = "https://pyrosafe-o880.onrender.com/Account/Register";
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
}

app.Run();