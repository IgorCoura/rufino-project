
namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class File : ValueObject
    {

        public string Path { get; private set; }
        public string Name { get; private set; }
        public FileExtesion Extension { get; private set; }
        public bool Valid { get; private set; }

        private File(string path, string name, FileExtesion extension, bool valid)
        {
            Path = path;
            Name = name;
            Extension = extension;
            Valid = valid;
        }

        public static File Create(string path, string name, FileExtesion extension, bool valid) => new(path, name, extension, valid);

        public string GetCompletePath() => System.IO.Path.Combine(Path, $"{Name}.{Extension}");

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Extension;
            yield return Path;
            yield return Name;
            yield return Valid;
        }
    }
}