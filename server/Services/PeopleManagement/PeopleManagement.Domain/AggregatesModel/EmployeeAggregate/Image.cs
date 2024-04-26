
namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class Image : ValueObject
    {

        public string Path { get; private set; }
        public string Name { get; private set; }
        public ImageExtesion Extension { get; private set; }
        public bool Valid { get; private set; }

        private Image(string path, string name, ImageExtesion extension, bool valid)
        {
            Path = path;
            Name = name;
            Extension = extension;
            Valid = valid;
        }

        public static Image Create(string path, string name, ImageExtesion extension, bool valid) => new(path, name, extension, valid);

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