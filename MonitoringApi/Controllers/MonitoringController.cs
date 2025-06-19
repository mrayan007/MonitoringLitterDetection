using Microsoft.AspNetCore.Mvc;
using MonitoringApi.Data;
using MonitoringApi.Models;
using MonitoringApi.DTOs; // Ensure all your DTOs are here (PredictionRequest, FrontendLocationResponse, etc.)
using System.Net.Http;
using System.Net.Http.Json; // For PostAsJsonAsync and ReadFromJsonAsync
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Text.Json; // Needed for JsonSerializer.Deserialize<JsonElement>
using System.Globalization;
using Microsoft.AspNetCore.Authorization; // ADD THIS USING DIRECTIVE FOR [Authorize]

namespace MonitoringApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Your endpoints will now start with /api/Monitoring/
    // [Authorize] // REMOVED: Do NOT apply [Authorize] at the class level if you want some methods public
    public class MonitoringController : ControllerBase
    {
        private readonly MonitoringContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        // Sensoring API configuration (remains the same)
        private readonly string _sensoringApiLoginPath;
        private readonly string _sensoringApiDataPath;
        private readonly string _sensoringApiLogoutPath;
        private readonly string _sensoringApiEmail;
        private readonly string _sensoringApiPassword;
        private readonly string _sensoringApiBaseUrl;

        // FASTAPI and LocationIQ API configuration (NEW)
        private readonly string _fastApiBaseUrl;
        private readonly string _locationIqBaseUrl;
        private readonly string _locationIqApiKey;


        public MonitoringController(MonitoringContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;

            // Sensoring API configuration (from your existing code)
            _sensoringApiBaseUrl = configuration.GetValue<string>("SensoringApi:BaseUrl")
                                   ?? throw new InvalidOperationException("Sensoring API BaseUrl is not configured in appsettings.json or user secrets.");
            _sensoringApiLoginPath = configuration.GetValue<string>("SensoringApi:LoginPath")
                                   ?? throw new InvalidOperationException("Sensoring API LoginPath is not configured in appsettings.json or user secrets.");
            _sensoringApiDataPath = configuration.GetValue<string>("SensoringApi:DataPath")
                                   ?? throw new InvalidOperationException("Sensoring API DataPath is not configured in appsettings.json or user secrets.");
            _sensoringApiLogoutPath = configuration.GetValue<string>("SensoringApi:LogoutPath")
                                   ?? throw new InvalidOperationException("Sensoring API LogoutPath is not configured in appsettings.json or user secrets.");
            _sensoringApiEmail = configuration.GetValue<string>("SensoringApi:Email")
                                   ?? throw new InvalidOperationException("Sensoring API Email is not configured in appsettings.json or user secrets.");
            _sensoringApiPassword = configuration.GetValue<string>("SensoringApi:Password")
                                   ?? throw new InvalidOperationException("Sensoring API Password is not configured in appsettings.json or user secrets.");

            // FastAPI and LocationIQ API configuration (NEWLY ADDED)
            _fastApiBaseUrl = configuration.GetValue<string>("FastApiBaseUrl")
                              ?? throw new InvalidOperationException("FastApiBaseUrl is not configured in appsettings.json or user secrets.");
            _locationIqBaseUrl = configuration.GetValue<string>("LocationIq:BaseUrl")
                                   ?? throw new InvalidOperationException("LocationIq:BaseUrl is not configured in appsettings.json or user secrets.");
            _locationIqApiKey = configuration.GetValue<string>("LocationIq:ApiKey")
                                ?? throw new InvalidOperationException("LocationIq:ApiKey is not configured in appsettings.json or user secrets.");
        }

        // POST: api/Monitoring/FetchAndStoreSensoringData
        // This endpoint remains public (no [Authorize] here)
        [HttpPost("FetchAndStoreSensoringData")]
        public async Task<IActionResult> FetchAndStoreSensoringData()
        {
            // Use the named client configured in Program.cs
            var sensoringApiClient = _httpClientFactory.CreateClient("SensoringApiClient");
            // No need to set BaseAddress here again, it's done by AddHttpClient

            string authToken = null;
            int newLitterCount = 0;

            // Step 1: Login and Get Token
            try
            {
                var loginRequest = new LoginRequestDto
                {
                    Email = _sensoringApiEmail,
                    Password = _sensoringApiPassword
                };

                Console.WriteLine($"DEBUG: Attempting login to Sensoring API: {sensoringApiClient.BaseAddress}{_sensoringApiLoginPath}");
                var loginResponse = await sensoringApiClient.PostAsJsonAsync(_sensoringApiLoginPath, loginRequest);
                loginResponse.EnsureSuccessStatusCode();

                var loginData = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
                authToken = loginData?.AccessToken;

                if (string.IsNullOrEmpty(authToken))
                {
                    Console.Error.WriteLine("ERROR: Sensoring API login returned no token or an empty token.");
                    return StatusCode(500, "Could not obtain authentication token from Sensoring API.");
                }
                Console.WriteLine("DEBUG: Sensoring API login successful, token received.");
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"ERROR: HttpRequestException during Sensoring API login: {ex.Message}");
                return StatusCode(500, $"Error logging in to Sensoring API: {ex.Message}. Check URL, credentials, and API response.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ERROR: Unexpected error during Sensoring API login: {ex.Message}");
                return StatusCode(500, $"Unexpected error during Sensoring API login: {ex.Message}");
            }

            // Step 2: Fetch Data with Token
            List<SensoringLitterDto> sensoringDataList = new List<SensoringLitterDto>();
            try
            {
                sensoringApiClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

                Console.WriteLine($"DEBUG: Attempting to retrieve data from Sensoring API: {sensoringApiClient.BaseAddress}{_sensoringApiDataPath}");
                var dataResponse = await sensoringApiClient.GetAsync(_sensoringApiDataPath);
                dataResponse.EnsureSuccessStatusCode();

                sensoringDataList = await dataResponse.Content.ReadFromJsonAsync<List<SensoringLitterDto>>();

                if (sensoringDataList == null || !sensoringDataList.Any())
                {
                    Console.WriteLine("INFO: No new sensoring data received from the API.");
                }
                else
                {
                    Console.WriteLine($"DEBUG: {sensoringDataList.Count} sensoring items received.");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"ERROR: HttpRequestException during data retrieval from Sensoring API: {ex.Message}");
                return StatusCode(500, $"Error retrieving data from Sensoring API: {ex.Message}. Check data path and token validity.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ERROR: Unexpected error during data retrieval from Sensoring API: {ex.Message}");
                return StatusCode(500, $"Unexpected error during data retrieval from Sensoring API: {ex.Message}");
            }
            finally
            {
                sensoringApiClient.DefaultRequestHeaders.Authorization = null; // Clear the Authorization header
            }

            // Step 3: Save Data to Litter table
            foreach (var sensoringDto in sensoringDataList)
            {
                if (await _context.Litter.AnyAsync(l => l.Id == sensoringDto.Id))
                {
                    Console.WriteLine($"INFO: Litter with Id {sensoringDto.Id} already exists in the database, skipping.");
                    continue;
                }

                var litter = new Litter
                {
                    Id = sensoringDto.Id == Guid.Empty ? Guid.NewGuid() : sensoringDto.Id,
                    DateTime = sensoringDto.DateTime,
                    LocationLat = sensoringDto.LocationLat,
                    LocationLon = sensoringDto.LocationLon,
                    Category = sensoringDto.Category,
                    Confidence = sensoringDto.Confidence,
                    Temperature = sensoringDto.Temperature
                };

                _context.Litter.Add(litter);
                await _context.SaveChangesAsync();
                newLitterCount++;
            }
            Console.WriteLine($"DEBUG: {newLitterCount} new sensoring items saved to Litter table.");

            // Step 4: Logout
            if (!string.IsNullOrEmpty(authToken))
            {
                try
                {
                    Console.WriteLine($"DEBUG: Attempting logout from Sensoring API: {sensoringApiClient.BaseAddress}{_sensoringApiLogoutPath}");
                    var logoutResponse = await sensoringApiClient.PostAsync(_sensoringApiLogoutPath, null);
                    logoutResponse.EnsureSuccessStatusCode();
                    Console.WriteLine("DEBUG: Successfully logged out from Sensoring API.");
                }
                catch (HttpRequestException ex)
                {
                    Console.Error.WriteLine($"ERROR: HttpRequestException during Sensoring API logout: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"ERROR: Unexpected error during Sensoring API logout: {ex.Message}");
                }
            }

            return Ok($"Successfully fetched and saved {newLitterCount} new sensoring items to the Litter table. Logout process completed.");
        }

        // Endpoint to predict location
        [HttpPost("predict/location")] // Calls FastAPI's /predict/location
        [Authorize] // ADDED: This endpoint now requires a valid JWT
        public async Task<IActionResult> PredictLocation([FromBody] PredictionRequest request)
        {
            // Use the named client configured in Program.cs
            var fastApiClient = _httpClientFactory.CreateClient("FastApiClient");
            // No need to set BaseAddress here again.

            try
            {
                // 1. Call FastAPI's /predict/location endpoint
                var fastApiRequestContent = new StringContent(
                    JsonSerializer.Serialize(request),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                Console.WriteLine($"DEBUG: Calling FastAPI location prediction: {fastApiClient.BaseAddress}/predict/location");
                var fastApiResponse = await fastApiClient.PostAsync("/predict/location", fastApiRequestContent);

                if (!fastApiResponse.IsSuccessStatusCode)
                {
                    var errorContent = await fastApiResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"ERROR: FastAPI Location Error: {fastApiResponse.StatusCode} - {errorContent}");
                    return StatusCode((int)fastApiResponse.StatusCode, JsonSerializer.Deserialize<JsonElement>(errorContent));
                }

                var fastApiLocation = await fastApiResponse.Content.ReadFromJsonAsync<FastApiLocationPredictionResponse>();

                if (fastApiLocation == null)
                {
                    Console.WriteLine("ERROR: FastAPI returned null for location prediction.");
                    return StatusCode(500, new { detail = "FastAPI returned null location prediction." });
                }

                // 2. Call LocationIQ for reverse geocoding
                // Use the named client configured in Program.cs
                var locationIqClient = _httpClientFactory.CreateClient("LocationIqApiClient");
                // The URL here is relative to the BaseAddress set in Program.cs for "LocationIqApiClient"
                var locationIqUrl = $"reverse.php?key={_locationIqApiKey}&lat={fastApiLocation.Latitude.ToString(CultureInfo.InvariantCulture)}&lon={fastApiLocation.Longitude.ToString(CultureInfo.InvariantCulture)}&format=json";

                Console.WriteLine($"DEBUG: Calling LocationIQ: {locationIqClient.BaseAddress}{locationIqUrl}");
                var locationIqResponse = await locationIqClient.GetAsync(locationIqUrl);

                if (!locationIqResponse.IsSuccessStatusCode)
                {
                    var errorContent = await locationIqResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"ERROR: LocationIQ Error: {locationIqResponse.StatusCode} - {errorContent}");
                    return StatusCode((int)locationIqResponse.StatusCode, new { detail = "Failed to reverse geocode location from LocationIQ." });
                }

                var locationIqData = await locationIqResponse.Content.ReadFromJsonAsync<LocationIqReverseGeocodeResponseDto>();

                // 3. Return combined data to frontend
                return Ok(new FrontendLocationResponse
                {
                    Latitude = fastApiLocation.Latitude,
                    Longitude = fastApiLocation.Longitude,
                    Address = locationIqData?.DisplayName ?? $"Lat: {fastApiLocation.Latitude}, Lon: {fastApiLocation.Longitude}"
                });
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"ERROR: HttpRequestException during location prediction: {ex.Message}");
                return StatusCode(500, $"Error communicating with external API: {ex.Message}. Check URLs and network connection.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ERROR: Unexpected error during location prediction: {ex.Message}");
                return StatusCode(500, $"Internal server error during location prediction: {ex.Message}");
            }
        }

        // Endpoint to predict temperature
        [HttpPost("predict/temperature")] // Calls FastAPI's /predict/temperature
        [Authorize] // ADDED: This endpoint now requires a valid JWT
        public async Task<IActionResult> PredictTemperature([FromBody] PredictionRequest request)
        {
            // Use the named client configured in Program.cs
            var fastApiClient = _httpClientFactory.CreateClient("FastApiClient");
            // No need to set BaseAddress here again.

            try
            {
                // 1. Call FastAPI's /predict/temperature endpoint
                var fastApiRequestContent = new StringContent(
                    JsonSerializer.Serialize(request),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                Console.WriteLine($"DEBUG: Calling FastAPI temperature prediction: {fastApiClient.BaseAddress}/predict/temperature");
                var fastApiResponse = await fastApiClient.PostAsync("/predict/temperature", fastApiRequestContent);

                if (!fastApiResponse.IsSuccessStatusCode)
                {
                    var errorContent = await fastApiResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"ERROR: FastAPI Temperature Error: {fastApiResponse.StatusCode} - {errorContent}");
                    return StatusCode((int)fastApiResponse.StatusCode, JsonSerializer.Deserialize<JsonElement>(errorContent));
                }

                var fastApiTemperature = await fastApiResponse.Content.ReadFromJsonAsync<FastApiTemperaturePredictionResponse>();

                return Ok(new FrontendTemperatureResponse
                {
                    Prediction = fastApiTemperature.Prediction,
                    Unit = fastApiTemperature.Unit
                });
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"ERROR: HttpRequestException during temperature prediction: {ex.Message}");
                return StatusCode(500, $"Error communicating with external API: {ex.Message}. Check URLs and network connection.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ERROR: Unexpected error during temperature prediction: {ex.Message}");
                return StatusCode(500, $"Internal server error during temperature prediction: {ex.Message}");
            }
        }
    }
}