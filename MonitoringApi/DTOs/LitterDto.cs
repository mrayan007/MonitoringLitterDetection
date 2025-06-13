using System.ComponentModel.DataAnnotations;

namespace MonitoringApi.DTOs
{
    public class LitterDto
    {
        [Required]
        public float Confidence { get; set; }
        [Required]
        public DateTime Timestamp { get; set; }
        [Required]
        public string Label { get; set; }
        public float? LocationLat { get; set; }
        public float? LocationLon { get; set; }
    }
}
