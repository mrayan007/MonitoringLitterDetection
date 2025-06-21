using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonitoringApi.Data; // Your DbContext
using System;
using System.Linq;

namespace MonitoringApi.IntegrationTests
{
    // This factory helps create an in-memory test server for your API
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(DbContextOptions<MonitoringContext>)); if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add an in-memory database for testing
                services.AddDbContext<MonitoringContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting"); // Unique name for each test run if needed
                });

                // Build the service provider.
                var sp = services.BuildServiceProvider();

                // Create a scope to obtain a reference to the database contexts
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<MonitoringContext>();
                    var logger = scopedServices.GetRequiredService<ILogger<CustomWebApplicationFactory<TProgram>>>();

                    // Ensure the database is created (for in-memory, this effectively means resetting it)
                    db.Database.EnsureDeleted(); // Clear previous data
                    db.Database.EnsureCreated(); // Create the schema

                    // Optionally, seed the database with test data here
                    try
                    {
                        // Example: db.Users.Add(new User { Username = "testuser", ... });
                        // db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "An error occurred seeding the " +
                                            "database with test messages. Error: {Message}", ex.Message);
                    }
                }
            });
        }
    }
}