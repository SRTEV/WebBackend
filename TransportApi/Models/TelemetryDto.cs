namespace TransportApi.Models
{
    public class TelemetryDto
    {
        public string QrCode { get; set; } = string.Empty;
        public double Battery { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Speed { get; set; }
    }
}