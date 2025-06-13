using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringApi.Models
{
    public class Litter
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public float Confidence { get; set; }
        public DateTime Timestamp { get; set; }
        public string Label { get; set; }
        public float? LocationLat { get; set; }
        public float? LocationLon { get; set; }
    }
}
