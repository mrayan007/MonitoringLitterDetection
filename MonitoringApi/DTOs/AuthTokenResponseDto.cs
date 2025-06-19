namespace MonitoringApi.DTOs
{
    public class AuthTokenResponseDto
    {
        public string AccessToken { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}