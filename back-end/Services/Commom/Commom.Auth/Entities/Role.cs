namespace Commom.Auth.Entities 
{ 

    public class Role
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Name { get; set; } = string.Empty;
        public IEnumerable<FunctionId> FunctionsIds { get; set; } = new List<FunctionId>();
    }
}
