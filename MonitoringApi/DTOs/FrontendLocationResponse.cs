namespace MonitoringApi.DTOs
{
    public class FrontendLocationResponse
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; } // The human-readable address
    }
}
