using MonitoringApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
// Removed: using System.Text.Json; // No longer needed if JsonNamingPolicy is not used

// IMPORTANT: Wrap your existing code in a public partial class Program
public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // --- 1. Services to the container ---

        var jwtSettings = builder.Configuration.GetSection("Jwt");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"]);

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false; // Set to true in production!
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // No leeway for expiration
            };
        });

        builder.Services.AddAuthorization();

        // AddControllers without JsonOptions. This will use the default JSON serialization (PascalCase).
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        });


        builder.Services.AddEndpointsApiExplorer();
        //builder.Services.AddSwaggerGen(); // Uncomment to enable Swagger UI for API documentation

        // Configure DbContext with SQL Server
        builder.Services.AddDbContext<MonitoringContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Configure named HttpClient for LocationIQ API
        builder.Services.AddHttpClient("LocationIqApiClient", client =>
        {
            var baseUrl = builder.Configuration.GetValue<string>("LocationIq:BaseUrl");
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new InvalidOperationException("LocationIQ API BaseUrl is not configured in appsettings.json or user secrets.");
            }
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(10); // Standard timeout
        });

        // Configure named HttpClient for Sensoring API
        builder.Services.AddHttpClient("SensoringApiClient", client =>
        {
            var baseUrl = builder.Configuration.GetValue<string>("SensoringApi:BaseUrl");
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new InvalidOperationException("Sensoring API BaseUrl is not configured in appsettings.json or user secrets.");
            }
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30); // Standard timeout
        });

        // Configure named HttpClient for FastAPI
        builder.Services.AddHttpClient("FastApiClient", client =>
        {
            var baseUrl = builder.Configuration.GetValue<string>("FastApiBaseUrl"); // Key name as per your appsettings.json
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new InvalidOperationException("FastAPI BaseUrl is not configured in appsettings.json or user secrets.");
            }
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(15); // Standard timeout
        });

        // --- IMPORTANT: Configure CORS for your frontend ---
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(
                policy =>
                {
                    // Add the exact URL(s) where your frontend comes from.
                    // These should match your VS Code Live Server or other local development server URLs.
                    policy.WithOrigins(
                            "http://127.0.0.1:5500", // Common for VS Code Live Server
                            "http://localhost:5500", // Common for VS Code Live Server (alternative)
                            "http://localhost:3000", // Example for React/Vue/Angular dev servers
                            "http://127.0.0.1:3000", // Your confirmed frontend URL
                            "http://127.0.0.1:8000"  // FastAPI's own URL, for dev purposes if needed
                        )
                        .AllowAnyHeader()   // Allow all headers
                        .AllowAnyMethod();  // Allow all HTTP methods (GET, POST, etc.)
                });
        });

        var app = builder.Build();

        // --- 2. Configure the HTTP request pipeline ---

        // Enable Swagger/SwaggerUI in development environment
        //if (app.Environment.IsDevelopment())
        //{
        //    app.UseSwagger();
        //    app.UseSwaggerUI();
        //}

        app.UseHttpsRedirection();

        // --- IMPORTANT: Use CORS policy ---
        // This must be called BEFORE UseAuthorization and MapControllers
        app.UseCors(); // Activates the default CORS policy defined above

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Run database migrations on startup
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MonitoringContext>();
            Console.WriteLine("Applying database migrations...");
            try
            {
                dbContext.Database.Migrate();
                Console.WriteLine("Database migrations applied successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying database migrations: {ex.Message}");
                // Consider using a proper logging framework here instead of Console.WriteLine
            }
        }

        app.Run();
    }
}