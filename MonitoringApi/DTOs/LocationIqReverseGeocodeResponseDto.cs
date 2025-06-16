using System.Text.Json.Serialization;

namespace MonitoringApi.DTOs
{
    public class LocationIqReverseGeocodeResponseDto
    {
        [JsonPropertyName("lat")]
        public string Lat { get; set; }

        [JsonPropertyName("lon")]
        public string Lon { get; set; }

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }
    }
}