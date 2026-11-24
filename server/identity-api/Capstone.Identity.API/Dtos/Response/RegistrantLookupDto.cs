namespace Capstone.Identity.API.Dtos.Response
{
    // communicates whether a given participant is already registered in the event
    public class RegistrantLookupDto
    {
        public bool Found { get; set; }
        public Guid? UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? DisplayName { get; set; }
        public string? AccountStatus { get; set; }
        public bool AlreadyEnrolled { get; set; }
    }
}