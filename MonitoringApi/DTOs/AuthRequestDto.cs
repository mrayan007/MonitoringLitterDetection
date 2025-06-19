using System.ComponentModel.DataAnnotations;

namespace MonitoringApi.DTOs
{
    public class AuthRequestDto
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}