using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringApi.Models
{
    public class EnrichedLitter
    {
        [Key]
        [ForeignKey("Litter")]
        [Required]
        public Guid Id { get; set; }
        public Litter Litter { get; set; }
        [Required]
        public DateTime DateTime { get; set; }
        [Required]
        public string Category { get; set; }
        [Required]
        public float Confidence { get; set; }
        [Required]
        public float Temperature { get; set; }
        public string? Location { get; set; }
    }
}