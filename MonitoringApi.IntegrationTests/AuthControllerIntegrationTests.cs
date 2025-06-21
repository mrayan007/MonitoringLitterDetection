using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Threading.Tasks;
using MonitoringApi; // Your API's namespace, usually the project name
using Newtonsoft.Json;
using System.Text;
using MonitoringApi.DTOs;

namespace MonitoringApi.IntegrationTests
{
    [TestClass]
    public class AuthControllerIntegrationTests
    {
        private static CustomWebApplicationFactory<Program> _factory;
        private HttpClient _client;

        [ClassInitialize] // Runs once before all tests in the class
        public static void ClassInitialize(TestContext context)
        {
            _factory = new CustomWebApplicationFactory<Program>();
        }

        [TestInitialize] // Runs before each test method
        public void TestInitialize()
        {
            _client = _factory.CreateClient();
        }

        [TestMethod]
        public async Task Login_ValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var loginRequest = new AuthRequestDto
            {
                Username = "admin", // Matches hardcoded credentials in AuthController for now
                Password = "password123"
            };
            var content = new StringContent(JsonConvert.SerializeObject(loginRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/Auth/login", content);

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            var responseString = await response.Content.ReadAsStringAsync();
            var responseDto = JsonConvert.DeserializeObject<AuthTokenResponseDto>(responseString);

            Assert.IsNotNull(responseDto);
            Assert.IsFalse(string.IsNullOrEmpty(responseDto.AccessToken));
            Assert.IsNotNull(responseDto.ExpiresAt);
        }

        [TestMethod]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginRequest = new AuthRequestDto
            {
                Username = "wronguser",
                Password = "wrongpassword"
            };
            var content = new StringContent(JsonConvert.SerializeObject(loginRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/Auth/login", content);

            // Assert
            Assert.AreEqual(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [ClassCleanup] // Runs once after all tests in the class
        public static void ClassCleanup()
        {
            _factory.Dispose(); // Clean up the test server
        }
    }
}