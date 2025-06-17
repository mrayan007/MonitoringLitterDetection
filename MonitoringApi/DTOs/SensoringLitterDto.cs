using System;
using System.Text.Json.Serialization;

namespace MonitoringApi.DTOs
{
    public class SensoringLitterDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("dateTime")]
        public DateTime DateTime { get; set; }

        [JsonPropertyName("locationLat")] // Kan "lat" of "locationLat" zijn
        public float LocationLat { get; set; }

        [JsonPropertyName("locationLon")] // Kan "lon" of "locationLon" zijn
        public float LocationLon { get; set; }

        [JsonPropertyName("category")] // Kan "category" of "wasteType" zijn
        public string Category { get; set; }

        [JsonPropertyName("confidence")]
        public float Confidence { get; set; }

        [JsonPropertyName("temperature")]
        public float Temperature { get; set; }
    }
}