namespace Application.Events
{
    public class NearbyEventParams
    {
        public string City { get; set; }
        public string Country { get; set; }

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
    }
}