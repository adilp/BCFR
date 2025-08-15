using Microsoft.EntityFrameworkCore;
using MemberOrgApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MemberOrgApi.Models;
using MemberOrgApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure application to use environment variables with proper prefix in production
if (builder.Environment.IsProduction())
{
    builder.Configuration.AddEnvironmentVariables();
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HttpClient factory for email service
builder.Services.AddHttpClient();

// Configure Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// In production, use DATABASE_URL from DigitalOcean
if (builder.Environment.IsProduction())
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrEmpty(databaseUrl))
    {
        // Parse DATABASE_URL format: postgresql://user:password@host:port/database
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true;Search Path=memberorg,public";
    }
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Configure the migrations history table to use our custom schema
        npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "memberorg");
        // Set default schema for production
        if (builder.Environment.IsProduction())
        {
            npgsqlOptions.SetPostgresVersion(new Version(15, 0));
        }
    }));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "MemberOrgApi",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "MemberOrgApp",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                builder.Configuration["Jwt:Key"] ?? "ThisIsAVerySecureKeyForDevelopment123456789012"))
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var token = context.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                
                // Check if session exists and is not expired
                var session = await dbContext.Sessions
                    .FirstOrDefaultAsync(s => s.Token == token && s.ExpiresAt > DateTime.UtcNow);
                
                if (session == null)
                {
                    context.Fail("Session not found or expired");
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Role-based policies
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("MemberOrHigher", policy => policy.RequireRole("Admin", "Member"));
    
    // Future: Add more granular policies
    // options.AddPolicy("CanEditEvents", policy => policy.RequireRole("Admin", "Marketing"));
    // options.AddPolicy("CanViewFinances", policy => policy.RequireRole("Admin", "Finance"));
});

// Configure Stripe
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
builder.Services.AddScoped<IStripeService, StripeService>();

// Configure Email Service
builder.Services.AddScoped<IEmailService, EmailService>();

// Register activity log service
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Add /api prefix for local development to match frontend expectations
if (app.Environment.IsDevelopment())
{
    app.UsePathBase("/api");
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Run migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Running database migrations...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database");
        // Don't throw - let the app start even if migrations fail
        // This allows you to debug the issue
    }
}

app.Run();