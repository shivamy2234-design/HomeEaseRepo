namespace HomeEase.Models
{
    public class ServiceViewModel
    {
        public int ServiceId { get; set; }

        public string ServiceName { get; set; }

        public string Category { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public int DurationMinutes { get; set; }
    }
}
