using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonitoringApi.Controllers;
using Microsoft.Extensions.Configuration;
using Moq;
using MonitoringApi.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace MonitoringApi.UnitTests.Controllers
{
    [TestClass]
    public class AuthControllerTests
    {
        private Mock<IConfiguration> _mockConfiguration;
        private Mock<IConfigurationSection> _mockJwtSection;

        [TestInitialize] // This method runs before each test
        public void Setup()
        {
            // Mock the IConfiguration to control JWT settings
            _mockConfiguration = new Mock<IConfiguration>();
            _mockJwtSection = new Mock<IConfigurationSection>();

            // Setup Jwt section values
            _mockJwtSection.Setup(s => s["Secret"]).Returns("ThisIsAVerySecureSecretKeyForTesting123"); // Needs to be long enough for HmacSha256
            _mockJwtSection.Setup(s => s["Issuer"]).Returns("TestIssuer");
            _mockJwtSection.Setup(s => s["Audience"]).Returns("TestAudience");
            _mockJwtSection.Setup(s => s["TokenLifetimeMinutes"]).Returns("1"); // Short lifetime for tests

            // Link the "Jwt" section to the main configuration mock
            _mockConfiguration.Setup(c => c.GetSection("Jwt")).Returns(_mockJwtSection.Object);
        }

        [TestMethod]
        public void Login_ValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var controller = new AuthController(_mockConfiguration.Object);
            var model = new AuthRequestDto { Username = "admin", Password = "password123" };

            // Act
            var result = controller.Login(model) as OkObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(200, result.StatusCode);

            var responseDto = result.Value as AuthTokenResponseDto;
            Assert.IsNotNull(responseDto);
            Assert.IsFalse(string.IsNullOrEmpty(responseDto.AccessToken));
            Assert.IsNotNull(responseDto.ExpiresAt);

            // Optional: Validate the token's content (e.g., issuer, audience)
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.ReadToken(responseDto.AccessToken) as JwtSecurityToken;
            Assert.IsNotNull(token);
            Assert.AreEqual("TestIssuer", token.Issuer);
            Assert.AreEqual("TestAudience", token.Audiences.First());
            Assert.AreEqual("admin", token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        }

        [TestMethod]
        public void Login_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var controller = new AuthController(_mockConfiguration.Object);
            var model = new AuthRequestDto { Username = "wronguser", Password = "wrongpassword" };

            // Act
            var result = controller.Login(model) as UnauthorizedObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(401, result.StatusCode);
            // Optionally check the message: Assert.AreEqual("Invalid credentials", (result.Value as dynamic).message);
        }

        // Add more unit tests for AuthController as needed
    }
}