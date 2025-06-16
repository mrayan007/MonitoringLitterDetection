using Microsoft.AspNetCore.Mvc;
using MonitoringApi.Data;
using MonitoringApi.Models;
using MonitoringApi.DTOs; // Zorg dat deze namespaces bestaan en correct zijn
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace MonitoringApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MonitoringController : ControllerBase
    {
        private readonly MonitoringContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration; // Nodig voor het ophalen van LocationIQ token
        private readonly string _locationIqAccessToken; // LocationIQ token

        public MonitoringController(MonitoringContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            // Haal de LocationIQ Access Token op uit appsettings.json
            _locationIqAccessToken = configuration.GetValue<string>("LocationIQApi:AccessToken")
                                     ?? throw new InvalidOperationException("LocationIQ API Access Token is niet geconfigureerd in appsettings.json.");
        }

        // POST: api/Monitoring/ReceiveLitterData
        // Endpoint om afvaldata te ontvangen (bijv. van een Sensoring API of voor testen)
        [HttpPost("ReceiveLitterData")]
        public async Task<IActionResult> ReceiveLitterData([FromBody] LitterDto litterDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // Retourneer fouten als het model niet valide is
            }

            // 1. Converteer DTO naar Model en sla de ruwe Litter data op
            var litter = new Litter
            {
                Id = litterDto.Id == Guid.Empty ? Guid.NewGuid() : litterDto.Id, // Genereer een nieuwe GUID als Id leeg is
                DateTime = litterDto.DateTime, // VELDNAAM CORRECTIE: Timestamp -> DateTime
                LocationLat = litterDto.LocationLat,
                LocationLon = litterDto.LocationLon,
                Category = litterDto.Category, // VELDNAAM CORRECTIE: Label -> Category
                Confidence = litterDto.Confidence,
                Temperature = litterDto.Temperature
            };

            // Controleer of de Litter met deze Id al bestaat om duplicaten te voorkomen
            if (await _context.Litter.AnyAsync(l => l.Id == litter.Id))
            {
                return Conflict($"Litter met Id {litter.Id} bestaat al.");
            }

            _context.Litter.Add(litter);
            await _context.SaveChangesAsync(); // Sla op om de Id te bevestigen voor de Foreign Key relatie

            // 2. Data verrijken met LocationIQ API
            var enrichedLitter = await EnrichLitterWithLocationData(litter);

            // 3. Opslaan van de verrijkte data in de database
            // Controleer of EnrichedLitter al bestaat voor deze OriginalLitterId (Id in EnrichedLitter)
            if (await _context.EnrichedLitter.AnyAsync(el => el.Id == enrichedLitter.Id))
            {
                // Dit zou niet moeten gebeuren direct na het opslaan van Litter, maar voor de zekerheid.
                return Conflict($"Verrijkte data voor Litter met Id {enrichedLitter.Id} bestaat al.");
            }
            _context.EnrichedLitter.Add(enrichedLitter);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEnrichedLitter), new { id = enrichedLitter.Id }, enrichedLitter);
        }

        // POST: api/Monitoring/GenerateMockLitterData
        // Endpoint om een specifieke hoeveelheid mock data te genereren en op te slaan
        [HttpPost("GenerateMockLitterData")] // <--- ATTRIBUUT TOEGEVOEGD
        public async Task<IActionResult> GenerateMockLitterData([FromQuery] int count = 5)
        {
            if (count <= 0)
            {
                return BadRequest("Aantal mock data items moet groter zijn dan 0.");
            }

            var generatedCount = 0;
            var random = new Random();

            for (int i = 0; i < count; i++)
            {
                var litterId = Guid.NewGuid();
                // Coördinaten van Breda, Nederland (+/- kleine afwijking)
                var locationLat = (float)(51.57 + (random.NextDouble() - 0.5) * 0.1);
                var locationLon = (float)(4.77 + (random.NextDouble() - 0.5) * 0.1);
                var categories = new[] { "Plastic", "Paper", "Glass", "Metal", "Organic" };
                var randomCategory = categories[random.Next(categories.Length)];
                var confidence = (float)(random.NextDouble() * 0.3 + 0.7); // Tussen 0.7 en 1.0
                var temperature = (float)(random.NextDouble() * 15.0 + 5.0); // Tussen 5 en 20 graden Celsius

                var litter = new Litter
                {
                    Id = litterId,
                    DateTime = DateTime.UtcNow.AddHours(-random.Next(1, 72)), // Afval van de afgelopen 3 dagen
                    LocationLat = locationLat,
                    LocationLon = locationLon,
                    Category = randomCategory,
                    Confidence = confidence,
                    Temperature = temperature
                };

                // Voeg Litter toe aan context
                _context.Litter.Add(litter);
                await _context.SaveChangesAsync(); // Sla op om de Id voor de Foreign Key te garanderen

                // Verrijk direct en voeg toe aan context
                var enrichedLitter = await EnrichLitterWithLocationData(litter);
                _context.EnrichedLitter.Add(enrichedLitter);
                await _context.SaveChangesAsync(); // Sla verrijkte data op

                generatedCount++;
            }

            return Ok($"{generatedCount} mock Litter en EnrichedLitter items succesvol gegenereerd en opgeslagen.");
        }


        // Helper functie om de LocationIQ API aan te roepen en data te verrijken
        private async Task<EnrichedLitter> EnrichLitterWithLocationData(Litter litter)
        {
            var locationIqClient = _httpClientFactory.CreateClient("LocationIqApiClient");
            // Gebruik CultureInfo.InvariantCulture om een punt als decimaal scheidingsteken te forceren
            var requestUrl = $"reverse?key={_locationIqAccessToken}&lat={litter.LocationLat.ToString(CultureInfo.InvariantCulture)}&lon={litter.LocationLon.ToString(CultureInfo.InvariantCulture)}&format=json";

            LocationIqReverseGeocodeResponseDto locationData = null;
            string apiResponseContent = null;

            try
            {
                var response = await locationIqClient.GetAsync(requestUrl);

                apiResponseContent = await response.Content.ReadAsStringAsync();

                response.EnsureSuccessStatusCode();

                locationData = await response.Content.ReadFromJsonAsync<LocationIqReverseGeocodeResponseDto>();

                if (locationData == null)
                {
                    Console.WriteLine($"DEBUG: ReadFromJsonAsync retourneerde NULL voor LocationIqReverseGeocodeResponseDto voor Lat: {litter.LocationLat}, Lon: {litter.LocationLon}");
                }
                else if (string.IsNullOrEmpty(locationData.DisplayName))
                {
                    Console.WriteLine($"DEBUG: DisplayName in de LocationIQ respons is NULL of leeg voor Lat: {litter.LocationLat}, Lon: {litter.LocationLon}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"ERROR: HttpRequestException bij LocationIQ API aanroep voor Lat: {litter.LocationLat}, Lon: {litter.LocationLon}: {ex.Message}. URL: {requestUrl}");
                Console.Error.WriteLine($"RESPONS BIJ FOUT: {apiResponseContent}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ERROR: Onverwachte fout bij LocationIQ enrichment voor Lat: {litter.LocationLat}, Lon: {litter.LocationLon}: {ex.Message}");
                Console.Error.WriteLine($"RESPONS BIJ FOUT: {apiResponseContent}");
            }

            var enrichedLitter = new EnrichedLitter
            {
                Id = litter.Id,
                DateTime = litter.DateTime,
                Category = litter.Category,
                Confidence = litter.Confidence,
                Temperature = litter.Temperature,
                Location = locationData?.DisplayName
            };

            return enrichedLitter;
        }


        // GET: api/Monitoring/EnrichedLitterData
        // Endpoint om alle verrijkte data op te halen voor het Dashboard
        [HttpGet("EnrichedLitterData")]
        public async Task<ActionResult<IEnumerable<EnrichedLitter>>> GetEnrichedLitterData()
        {
            return await _context.EnrichedLitter.ToListAsync();
        }

        // GET: api/Monitoring/EnrichedLitterData/{id}
        // Endpoint om een specifieke verrijkte data entry op te halen
        [HttpGet("EnrichedLitterData/{id}")]
        public async Task<ActionResult<EnrichedLitter>> GetEnrichedLitter(Guid id) // ID is nu Guid
        {
            var entry = await _context.EnrichedLitter.FindAsync(id);
            if (entry == null)
            {
                return NotFound(); // Retourneer 404 als niet gevonden
            }
            return entry;
        }
    }
}