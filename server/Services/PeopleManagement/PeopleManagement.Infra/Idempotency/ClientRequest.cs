namespace PeopleManagement.Infra.Idempotency
{
    public record ClientRequest
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public DateTime Time { get; init; }
    }
}
