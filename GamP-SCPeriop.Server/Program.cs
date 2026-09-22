using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Server.Services;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using System.Text;
using System.Threading.RateLimiting;

// Configurar a licença gratuita do QuestPDF
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Secrets never live in the repo: on the server they come from appsettings.Production.json or environment variables.
// Fail at startup with a clear message instead of running with a missing or weak key.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is not set. On the server, add it to appsettings.Production.json (next to the app) " +
        "or set the ConnectionStrings__DefaultConnection environment variable.");

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException(
        "Jwt:Key is missing or shorter than 32 bytes. On the server, add it to appsettings.Production.json " +
        "or set the Jwt__Key environment variable (generate one with: openssl rand -base64 48).");

// No CORS policy: the Blazor client is served by this same server (same origin), so no other site may call the API.

// Login attempts: at most 20 per minute from one IP address (per-account lockout is in LoginAttemptTracker)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync("Demasiadas tentativas. Aguarde um minuto e tente novamente.", token);
    };
});

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<LoginAttemptTracker>();

// Errors in Production return a generic message (details only go to the server log)
builder.Services.AddProblemDetails();

// Add services to the container.

//builder.Services.AddControllers();
builder.Services.AddControllersWithViews().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddRazorPages();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<BadgeService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AccessService>();
builder.Services.AddScoped<DbSeeder>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,                           // Try 5 times
            maxRetryDelay: TimeSpan.FromSeconds(10),    // Wait up to 10s between tries
            errorNumbersToAdd: null)
        ));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // Test data: dotnet run --project GamP-SCPeriop.Server -- --seed (wipes the database first)
    if (app.Environment.IsDevelopment() && args.Contains("--seed"))
    {
        await scope.ServiceProvider.GetRequiredService<DbSeeder>().ResetAndSeedAsync();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Unhandled errors: generic 500 response, no stack traces or messages sent to the browser
    app.UseExceptionHandler();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.MapFallbackToFile("index.html");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Empty database: create the first admin from configuration (no default password baked into the code).
    // Set BootstrapAdmin:Email and BootstrapAdmin:Password (e.g. as environment variables BootstrapAdmin__Email / BootstrapAdmin__Password).
    if (!db.Users.Any())
    {
        var adminEmail = builder.Configuration["BootstrapAdmin:Email"];
        var adminPassword = builder.Configuration["BootstrapAdmin:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword) || adminPassword.Length < 8)
        {
            app.Logger.LogWarning("The database has no users. Set BootstrapAdmin:Email and BootstrapAdmin:Password (8+ characters) to create the first admin.");
        }
        else
        {
            db.Users.Add(new User
            {
                Email = adminEmail,
                FullName = builder.Configuration["BootstrapAdmin:FullName"] ?? "Administrador",
                Password = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Role = UserRole.Admin
            });
            db.SaveChanges();
            app.Logger.LogInformation("Created the first admin account {Email}.", adminEmail);
        }
    }
}

app.Run();
