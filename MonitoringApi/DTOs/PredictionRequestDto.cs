namespace MonitoringApi.DTOs
{
    public class PredictionRequestDto
    {
        public float LocationLat { get; set; }
        public float LocationLon { get; set; }
        public float Temperature { get; set; }
        public int IsHoliday { get; set; }
        public string DayOfWeek { get; set; }
        public string TimeOfDayCategory { get; set; }
    }
}
