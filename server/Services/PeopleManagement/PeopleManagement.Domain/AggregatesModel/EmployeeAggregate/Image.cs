
namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class Image : ValueObject
    {
        

        public string FileName { get; private set; }
        public Extension Extension { get; private set; }

        public Image(string fileName, Extension extension)
        {
            FileName = fileName;
            Extension = extension;
        }

        public static Image Create(string fileName, Extension extension) => new(fileName, extension);

        public string GetNameWithExtension => $"{FileName}.{Extension}";
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return FileName;
            yield return Extension;
        }
    }
}
