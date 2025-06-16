using MonitoringApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // Nodig voor IConfiguration
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(); // <-- SWAGGER WEER INGESCHAKELD

// Configureer DbContext
builder.Services.AddDbContext<MonitoringContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configureer de named HttpClient voor LocationIQ API
// Dit is de cruciale fix voor de 'BaseAddress' fout.
builder.Services.AddHttpClient("LocationIqApiClient", client =>
{
    // Haal de BaseUrl op uit configuratie (appsettings.json)
    var baseUrl = builder.Configuration.GetValue<string>("LocationIQApi:BaseUrl");
    if (string.IsNullOrEmpty(baseUrl))
    {
        throw new InvalidOperationException("LocationIQ API BaseUrl is niet geconfigureerd in appsettings.json.");
    }
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(10); // Optioneel: stel een timeout in
});

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();    // <-- SWAGGER WEER INGESCHAKELD
//    app.UseSwaggerUI();  // <-- SWAGGER WEER INGESCHAKELD
//}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Run database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MonitoringContext>();
    dbContext.Database.Migrate();
}

app.Run();