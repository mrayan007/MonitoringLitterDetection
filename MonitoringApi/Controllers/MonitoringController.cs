using Microsoft.AspNetCore.Mvc;
using MonitoringApi.Data;
using MonitoringApi.Models;
using MonitoringApi.DTOs;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Globalization; // Voor CultureInfo.InvariantCulture

namespace MonitoringApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MonitoringController : ControllerBase
    {
        private readonly MonitoringContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _locationIqAccessToken;
        private readonly string _sensoringApiLoginPath;
        private readonly string _sensoringApiDataPath;
        private readonly string _sensoringApiLogoutPath;
        private readonly string _sensoringApiEmail;
        private readonly string _sensoringApiPassword;

        public MonitoringController(MonitoringContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;

            // Haal LocationIQ API instellingen op
            _locationIqAccessToken = configuration.GetValue<string>("LocationIQApi:AccessToken")
                                     ?? throw new InvalidOperationException("LocationIQ API Access Token is niet geconfigureerd in appsettings.json.");

            // Haal Sensoring API instellingen op
            _sensoringApiLoginPath = configuration.GetValue<string>("SensoringApi:LoginPath")
                                     ?? throw new InvalidOperationException("Sensoring API LoginPath is niet geconfigureerd in appsettings.json.");
            _sensoringApiDataPath = configuration.GetValue<string>("SensoringApi:DataPath")
                                    ?? throw new InvalidOperationException("Sensoring API DataPath is niet geconfigureerd in appsettings.json.");
            _sensoringApiLogoutPath = configuration.GetValue<string>("SensoringApi:LogoutPath")
                                      ?? throw new InvalidOperationException("Sensoring API LogoutPath is niet geconfigureerd in appsettings.json.");
            _sensoringApiEmail = configuration.GetValue<string>("SensoringApi:Email")
                                 ?? throw new InvalidOperationException("Sensoring API Email is niet geconfigureerd in appsettings.json.");
            _sensoringApiPassword = configuration.GetValue<string>("SensoringApi:Password")
                                    ?? throw new InvalidOperationException("Sensoring API Password is niet geconfigureerd in appsettings.json.");
        }

        // POST: api/Monitoring/FetchAndStoreSensoringData
        // Dit endpoint haalt data op van een externe sensoring API (met authenticatie),
        // slaat deze op in de Litter tabel en verrijkt deze.
        [HttpPost("FetchAndStoreSensoringData")]
        public async Task<IActionResult> FetchAndStoreSensoringData()
        {
            var sensoringApiClient = _httpClientFactory.CreateClient("SensoringApiClient");
            string authToken = null;

            // Stap 1: Inloggen en Token Ophalen
            try
            {
                var loginRequest = new LoginRequestDto
                {
                    Email = _sensoringApiEmail,
                    Password = _sensoringApiPassword
                };

                Console.WriteLine($"DEBUG: Poging tot inloggen bij Sensoring API: {sensoringApiClient.BaseAddress}{_sensoringApiLoginPath}");
                var loginResponse = await sensoringApiClient.PostAsJsonAsync(_sensoringApiLoginPath, loginRequest);
                loginResponse.EnsureSuccessStatusCode(); // Werpt uitzondering bij HTTP foutcodes (bijv. 401, 403, 500)

                var loginData = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
                authToken = loginData?.AccessToken;

                if (string.IsNullOrEmpty(authToken))
                {
                    Console.Error.WriteLine("ERROR: Sensoring API login retourneerde geen token of een leeg token.");
                    return StatusCode(500, "Kon geen authenticatie token verkrijgen van Sensoring API.");
                }
                Console.WriteLine("DEBUG: Sensoring API login succesvol, token ontvangen.");
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"ERROR: HttpRequestException bij inloggen Sensoring API: {ex.Message}");
                return StatusCode(500, $"Fout bij inloggen bij Sensoring API: {ex.Message}. Controleer URL, referenties en API-respons.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ERROR: Onverwachte fout bij inloggen Sensoring API: {ex.Message}");
                return StatusCode(500, $"Onverwachte fout bij inloggen bij Sensoring API: {ex.Message}");
            }

            // Stap 2: Data Ophalen met Token
            List<SensoringLitterDto> sensoringDataList = new List<SensoringLitterDto>();
            try
            {
                // Stel de Authorization header in voor de data-aanroep
                sensoringApiClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

                Console.WriteLine($"DEBUG: Poging tot ophalen data van Sensoring API: {sensoringApiClient.BaseAddress}{_sensoringApiDataPath}");
                var dataResponse = await sensoringApiClient.GetAsync(_sensoringApiDataPath);
                dataResponse.EnsureSuccessStatusCode();

                sensoringDataList = await dataResponse.Content.ReadFromJsonAsync<List<SensoringLitterDto>>();

                if (sensoringDataList == null || !sensoringDataList.Any())
                {
                    Console.WriteLine("INFO: Geen nieuwe sensoring data ontvangen van de API.");
                    // Ga verder naar uitloggen, zelfs als er geen data was.
                }
                else
                {
                    Console.WriteLine($"DEBUG: {sensoringDataList.Count} sensoring items ontvangen.");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"ERROR: HttpRequestException bij ophalen data Sensoring API: {ex.Message}");
                return StatusCode(500, $"Fout bij ophalen data van Sensoring API: {ex.Message}. Controleer data path en token geldigheid.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ERROR: Onverwachte fout bij ophalen data Sensoring API: {ex.Message}");
                return StatusCode(500, $"Onverwachte fout bij ophalen data van Sensoring API: {ex.Message}");
            }
            finally
            {
                // Wis de Authorization header zodat toekomstige aanroepen (die niet gerelateerd zijn aan deze sessie)
                // geen verouderde token bevatten. Dit is goed gebruik, maar de token wordt hoe dan ook ongeldig gemaakt bij logout.
                sensoringApiClient.DefaultRequestHeaders.Authorization = null;
            }

            // Stap 3: Data Opslaan en Verrijken
            var newLitterCount = 0;
            foreach (var sensoringDto in sensoringDataList)
            {
                // Controleer of de Litter met deze Id al bestaat om duplicaten te voorkomen
                if (await _context.Litter.AnyAsync(l => l.Id == sensoringDto.Id))
                {
                    Console.WriteLine($"INFO: Litter met Id {sensoringDto.Id} bestaat al in de database (uit Sensoring API), overslaan.");
                    continue; // Sla dit item over als het al bestaat
                }

                var litter = new Litter
                {
                    Id = sensoringDto.Id == Guid.Empty ? Guid.NewGuid() : sensoringDto.Id, // Genereer nieuwe GUID als leeg
                    DateTime = sensoringDto.DateTime,
                    LocationLat = sensoringDto.LocationLat,
                    LocationLon = sensoringDto.LocationLon,
                    Category = sensoringDto.Category,
                    Confidence = sensoringDto.Confidence,
                    Temperature = sensoringDto.Temperature
                };

                _context.Litter.Add(litter);
                await _context.SaveChangesAsync(); // Sla op om Id te bevestigen

                var enrichedLitter = await EnrichLitterWithLocationData(litter);

                if (await _context.EnrichedLitter.AnyAsync(el => el.Id == enrichedLitter.Id))
                {
                    Console.WriteLine($"INFO: Verrijkte data voor Litter met Id {enrichedLitter.Id} bestaat al (uit Sensoring API), overslaan.");
                    continue;
                }
                _context.EnrichedLitter.Add(enrichedLitter);
                await _context.SaveChangesAsync();
                newLitterCount++;
            }

            // Stap 4: Uitloggen
            if (!string.IsNullOrEmpty(authToken)) // Log alleen uit als er een token was
            {
                try
                {
                    // De logout endpoint kan een POST met lege body zijn, of een GET, of een POST met een specifieke body.
                    // Pas de methode hieronder aan indien nodig voor jouw specifieke API.
                    Console.WriteLine($"DEBUG: Poging tot uitloggen bij Sensoring API: {sensoringApiClient.BaseAddress}{_sensoringApiLogoutPath}");
                    var logoutResponse = await sensoringApiClient.PostAsync(_sensoringApiLogoutPath, null); // Meestal een POST met lege body
                    logoutResponse.EnsureSuccessStatusCode(); // Werpt uitzondering bij HTTP foutcodes
                    Console.WriteLine("DEBUG: Succesvol uitgelogd bij Sensoring API.");
                }
                catch (HttpRequestException ex)
                {
                    Console.Error.WriteLine($"ERROR: HttpRequestException bij uitloggen Sensoring API: {ex.Message}");
                    // Log de fout, maar de data is al opgeslagen, dus de primaire taak is voltooid.
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"ERROR: Onverwachte fout bij uitloggen Sensoring API: {ex.Message}");
                }
            }

            return Ok($"Succesvol {newLitterCount} nieuwe sensoring items opgehaald, opgeslagen en verrijkt. Uitlogproces voltooid.");
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
                Console.WriteLine($"DEBUG: Roep LocationIQ API aan: {locationIqClient.BaseAddress}{requestUrl}");
                var response = await locationIqClient.GetAsync(requestUrl);

                apiResponseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"DEBUG: LocationIQ API Respons Status: {(int)response.StatusCode} {response.ReasonPhrase}");
                Console.WriteLine($"DEBUG: LocationIQ API Respons Content: {apiResponseContent}");

                response.EnsureSuccessStatusCode(); // Werpt uitzondering bij niet-succesvolle statuscodes (4xx, 5xx)

                locationData = await response.Content.ReadFromJsonAsync<LocationIqReverseGeocodeResponseDto>();

                if (locationData == null)
                {
                    Console.WriteLine($"INFO: ReadFromJsonAsync retourneerde NULL voor LocationIqReverseGeocodeResponseDto voor Lat: {litter.LocationLat}, Lon: {litter.LocationLon}. Dit kan duiden op een lege of onverwachte JSON-structuur.");
                }
                else if (string.IsNullOrEmpty(locationData.DisplayName))
                {
                    Console.WriteLine($"INFO: DisplayName in de LocationIQ respons is NULL of leeg voor Lat: {litter.LocationLat}, Lon: {litter.LocationLon}.");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"ERROR: HttpRequestException bij LocationIQ API aanroep voor Lat: {litter.LocationLat}, Lon: {litter.LocationLon}: {ex.Message}. URL: {locationIqClient.BaseAddress}{requestUrl}");
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
                Location = locationData?.DisplayName // Kan null zijn als LocationIQ fout ging
            };

            return enrichedLitter;
        }
    }
}