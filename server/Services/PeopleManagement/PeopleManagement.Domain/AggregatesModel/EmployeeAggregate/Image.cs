
namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class Image : ValueObject
    {

        public string Path { get; private set; }
        public string Name { get; private set; }
        public ImageExtesion Extension { get; private set; }

        private Image(string path, string name, ImageExtesion extension)
        {
            Path = path;
            Name = name;
            Extension = extension;
        }

        public static Image Create(string path, string name, ImageExtesion extension) => new(path, name, extension);

        public string GetCompletePath() => System.IO.Path.Combine(Path, $"{Name}.{Extension}");

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Extension;
            yield return Path;
            yield return Name;
        }
    }
}