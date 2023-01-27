namespace Application.Events
{
    public class EventDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public DateTime Date { get; set; } 
        public string Description { get; set; }
        public string Category { get; set; }
        public string Anonimity { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string Venue { get; set; }
        public string HostUsername { get; set; }
        public string HostDisplayName { get; set; }
        public bool IsCancelled { get; set; }
        public ICollection<AttendeeDto> Attendees { get; set; }
        public ICollection<InviteRequestDto> InviteRequests { get; set; }
    }
}