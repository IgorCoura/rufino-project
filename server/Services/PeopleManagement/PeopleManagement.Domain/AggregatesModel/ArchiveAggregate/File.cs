
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;

namespace PeopleManagement.Domain.AggregatesModel.ArchiveAggregate
{
    public sealed class File : ValueObject
    {
        public Name Name { get; private set; } = null!;
        public Extension Extension { get; private set; } = null!;
        public FileStatus Status { get; private set; } = FileStatus.Pending;
        public DateTime InsertAt { get; private set; }
        private File() { }

        private File(Name name, Extension extension, DateTime insertAt)
        {
            Name = name;
            Extension = extension;
            InsertAt = insertAt;
        }

        private File(Name name, Extension extension, DateTime insertAt, FileStatus status)
        {
            Name = name;
            Extension = extension;
            InsertAt = insertAt;
            Status = status;
        }

        public static File Create(Name name, Extension extension) => new(name, extension, DateTime.UtcNow);
        public static File CreateWithoutVerification(Name name, Extension extension) => new(name, extension, DateTime.UtcNow, FileStatus.OK);

        public bool RequiresVerification => Status == FileStatus.Pending;
        public void NotApplicable()
        {
            if (Status == FileStatus.Pending)
                Status = FileStatus.NotApplicable;
        }

        public string GetNameWithExtension => $"{Name}.{Extension}";
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            throw new NotImplementedException();
        }
    }
}
