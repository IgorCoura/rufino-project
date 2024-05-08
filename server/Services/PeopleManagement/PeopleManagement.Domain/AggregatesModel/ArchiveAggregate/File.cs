
namespace PeopleManagement.Domain.AggregatesModel.ArchiveAggregate
{
    public sealed class File : ValueObject
    {
        public string Name { get; private set; } = string.Empty;
        public string Extension { get; private set; } = string.Empty;
        public FileStatus Status { get; private set; } = FileStatus.Pending;
        public DateTime InsertAt { get; private set; }
        private File() { }

        private File(string name, string extension, DateTime insertAt)
        {
            Name = name;
            Extension = extension;
            InsertAt = insertAt;
        }

        private File(string name, string extension, DateTime insertAt, FileStatus status)
        {
            Name = name;
            Extension = extension;
            InsertAt = insertAt;
            Status = status;
        }

        public static File Create(string name, string extension, DateTime insertAt) => new(name, extension, insertAt);
        public static File CreateWithoutVerification(string name, string extension, DateTime insertAt) => new(name, extension, insertAt, FileStatus.OK);

        public bool RequiresVerification => Status == FileStatus.Pending;

        public string GetNameWithExtension => $"{Name}.{Extension}";
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            throw new NotImplementedException();
        }
    }
}
