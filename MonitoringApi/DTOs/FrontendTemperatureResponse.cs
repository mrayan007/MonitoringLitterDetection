namespace MonitoringApi.DTOs
{
    public class FrontendTemperatureResponse
    {
        public double Prediction { get; set; }
        public string Unit { get; set; } // e.g., "degrees Celsius"
    }

}
