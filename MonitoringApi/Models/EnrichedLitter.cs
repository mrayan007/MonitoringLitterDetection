using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MonitoringApi.Models
{
    public class EnrichedLitter
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int OriginalLitterId { get; set; } // Verwijst naar de originele Litter
        public float Confidence { get; set; }
        public DateTime Timestamp { get; set; }
        public string Label { get; set; }
        public float? LocationLat { get; set; }
        public float? LocationLon { get; set; }

        // Velden voor verrijkte data
        public string WeatherCondition { get; set; }
        public float? Temperature { get; set; }
        public int? IsHoliday { get; set; } // 0 voor geen vakantie, 1 voor wel
        public string DayOfWeek { get; set; }
        public string TimeOfDayCategory { get; set; }
    }
}
