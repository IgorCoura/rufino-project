namespace PeopleManagement.Domain.AggregatesModel.ArchiveAggregate
{
    public class Document : ValueObject
    {
        
        public string Name { get; private set; }

        private Document(string name)
        {
            Name = name;
        }

        public static implicit operator Document(string input) =>
         new(input);

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Name;
        }
    }
}
