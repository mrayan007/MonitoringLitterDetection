using System.ComponentModel.DataAnnotations;

namespace MonitoringApi.DTOs
{
    public class LitterDto
    {
        [Required]
        public Guid Id { get; set; }
        [Required]
        public DateTime DateTime { get; set; }
        [Required]
        public float LocationLat { get; set; }
        [Required]
        public float LocationLon { get; set; }
        [Required]
        public string Category { get; set; }
        [Required]
        public float Confidence { get; set; }
        [Required]
        public float Temperature { get; set; }
    }
}