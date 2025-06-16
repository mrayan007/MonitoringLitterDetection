//using Microsoft.AspNetCore.Mvc;
//using MonitoringApi.Data;
//using MonitoringApi.Models;
//using MonitoringApi.DTOs;
//using System.Net.Http;
//using System.Net.Http.Json;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Configuration;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Microsoft.EntityFrameworkCore;

//namespace MonitoringApi.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class MonitoringController : ControllerBase
//    {
//        private readonly MonitoringContext _context;
//        private readonly IHttpClientFactory _httpClientFactory;
//        private readonly string _predictionApiBaseUrl;
//        private readonly IWebHostEnvironment _env;

//        public MonitoringController(MonitoringContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration, IWebHostEnvironment env)
//        {
//            _context = context;
//            _httpClientFactory = httpClientFactory;
//            _predictionApiBaseUrl = configuration.GetValue<string>("PredictionApiBaseUrl");
//            _env = env;
//        }

//        // POST: api/Monitoring/receive-trash-data
//        // Endpoint om geaggregeerde afvaldata te ontvangen van de Sensoring API
//        [HttpPost("ReceiveLitterData")]
//        public async Task<IActionResult> ReceiveLitterData([FromBody] LitterDto litterDto)
//        {
//            if (!ModelState.IsValid)
//            {
//                return BadRequest(ModelState); // Retourneer fouten als het model niet valide is
//            }

//            // 1. Opslaan van de ruwe data in de database
//            var litter= new Litter
//            {
//                Confidence = litterDto.Confidence,
//                Timestamp = litterDto.Timestamp,
//                Label = litterDto.Label,
//                LocationLat = litterDto.LocationLat,
//                LocationLon = litterDto.LocationLon,
//            };

//            _context.Litter.Add(litter);
//            await _context.SaveChangesAsync(); // Sla op om de Id voor de Foreign Key te krijgen

//            // 2. Data verrijken met externe API (PLACEHOLDER LOGICA)
//            // HIER moet je je logica implementeren voor het aanroepen van ECHTE externe API's (bijv. weer, feestdagen).
//            // Dit voorbeeld gebruikt dummy-waarden.
//            var enrichedData = new EnrichedLitter
//            {
//                OriginalLitterId = litter.Id,
//                Confidence = litter.Confidence,
//                Timestamp = litter.Timestamp,
//                Label = litter.Label,
//                LocationLat = litter.LocationLat,
//                LocationLon = litter.LocationLon,
//                WeatherCondition = "Bewolkt", // Voorbeeld waarde van externe weer API
//                Temperature = 15.5f,        // Voorbeeld waarde
//                IsHoliday = (litter.Timestamp.DayOfWeek == DayOfWeek.Saturday || litter.Timestamp.DayOfWeek == DayOfWeek.Sunday) ? 1 : 0, // Simpele check voor weekend
//                DayOfWeek = litter.Timestamp.DayOfWeek.ToString(),
//                TimeOfDayCategory = GetTimeOfDayCategory(litter.Timestamp.Hour) // Helper functie voor tijdscategorie
//            };

//            // 3. Opslaan van de verrijkte data in de database
//            _context.EnrichedLitter.Add(enrichedData);
//            await _context.SaveChangesAsync();

//            // Retourneer 201 Created status met de URL van de nieuw aangemaakte resource
//            return CreatedAtAction(nameof(GetEnrichedLitter), new { id = enrichedData.Id }, enrichedData);
//        }

//        // Helper functie om de tijdscategorie te bepalen
//        private string GetTimeOfDayCategory(int hour)
//        {
//            if (hour >= 0 && hour < 6) return "Night";
//            if (hour >= 6 && hour < 12) return "Morning";
//            if (hour >= 12 && hour < 18) return "Afternoon";
//            return "Evening";
//        }

//        // GET: api/Monitoring/enriched-trash-data
//        // Endpoint om alle verrijkte data op te halen voor het Dashboard
//        [HttpGet("EnrichedLitterData")]
//        public async Task<ActionResult<IEnumerable<EnrichedLitter>>> GetEnrichedLitterData()
//        {
//            return await _context.EnrichedLitter.ToListAsync();
//        }

//        // GET: api/Monitoring/enriched-trash-data/{id}
//        // Endpoint om een specifieke verrijkte data entry op te halen
//        [HttpGet("EnrichedLitterData/{id}")]
//        public async Task<ActionResult<EnrichedLitter>> GetEnrichedLitter(int id)
//        {
//            var entry = await _context.EnrichedLitter.FindAsync(id);
//            if (entry == null)
//            {
//                return NotFound(); // Retourneer 404 als niet gevonden
//            }
//            return entry;
//        }

//        // POST: api/Monitoring/predict-trash
//        // Endpoint om voorspellingen aan te vragen bij de FastAPI Trash Prediction API
//        [HttpPost("PredictLitter")]
//        public async Task<IActionResult> PredictLitter([FromBody] PredictionRequestDto requestDto)
//        {
//            if (!ModelState.IsValid)
//            {
//                return BadRequest(ModelState);
//            }

//            // MOCK LOGICA: Retourneer een hardgecodeerde respons in de ontwikkelomgeving
//            if (_env.IsDevelopment()) // Controleer of we in ontwikkelmodus zijn
//            {
//                Console.WriteLine("MOCK API: Returning hardcoded prediction for testing.");
//                var mockPredictionResponse = new PredictionResponseDto
//                {
//                    PredictedPriority = 1 // Of een andere mockwaarde (bijv. 2 of 3)
//                };
//                return Ok(mockPredictionResponse);
//            }

//            // OORSPRONKELIJKE LOGICA: Roep de echte FastAPI aan (voor productie/wanneer FastAPI beschikbaar is)
//            if (string.IsNullOrEmpty(_predictionApiBaseUrl))
//            {
//                return StatusCode(500, "Prediction API base URL is not configured. Check appsettings.json or Azure settings.");
//            }

//            var httpClient = _httpClientFactory.CreateClient();
//            try
//            {
//                var response = await httpClient.PostAsJsonAsync($"{_predictionApiBaseUrl}/predict", requestDto);
//                response.EnsureSuccessStatusCode();
//                var predictionResponse = await response.Content.ReadFromJsonAsync<PredictionResponseDto>();
//                return Ok(predictionResponse);
//            }
//            catch (HttpRequestException e)
//            {
//                Console.WriteLine($"Error calling Prediction API: {e.Message}. Check if Prediction API is running and accessible at {_predictionApiBaseUrl}.");
//                return StatusCode(500, $"Error communicating with Prediction API: {e.Message}. Check if Prediction API is running and accessible at {_predictionApiBaseUrl}.");
//            }
//            catch (Exception e)
//            {
//                Console.WriteLine($"An unexpected error occurred: {e.Message}");
//                return StatusCode(500, $"An unexpected error occurred: {e.Message}");
//            }
//        }
//    }
//}