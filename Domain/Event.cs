namespace Domain
{
    public class Event
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
        public bool IsCancelled { get; set; }
        public ICollection<EventAttendee> Attendees { get; set; } = new List<EventAttendee>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<InviteRequest> InviteRequests { get; set; } = new List<InviteRequest>();
    }
}