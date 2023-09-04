namespace Commom.Auth.Entities
{
    public class FunctionId
    {
        public Guid Id{ get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Name { get; set; } = string.Empty;
        public IEnumerable<Role> Roles { get; set; } = new List<Role>();
    }
}

