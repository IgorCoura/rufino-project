
namespace PeopleManagement.Domain.AggregatesModel.ArchiveAggregate
{
    public sealed class File : ValueObject
    {
        public Name Name { get; private set; } = null!;
        public Extension Extension { get; private set; } = null!;
        public DocumentStatus Status { get; private set; } = DocumentStatus.Pending;
        public DateTime InsertAt { get; private set; }
        private File() { }

        private File(Name name, Extension extension, DateTime insertAt)
        {
            Name = name;
            Extension = extension;
            InsertAt = insertAt;
        }

        private File(Name name, Extension extension, DateTime insertAt, DocumentStatus status)
        {
            Name = name;
            Extension = extension;
            InsertAt = insertAt;
            Status = status;
        }

        public static File Create(Name name, Extension extension, DateTime insertAt) => new(name, extension, insertAt);
        public static File CreateWithoutVerification(Name name, Extension extension, DateTime insertAt) => new(name, extension, insertAt, DocumentStatus.OK);

        public bool RequiresVerification => Status == DocumentStatus.Pending;

        public string GetNameWithExtension => $"{Name}.{Extension}";
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            throw new NotImplementedException();
        }
    }
}
