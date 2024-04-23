using PeopleManagement.Domain.Exceptions;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class Testimonial : ValueObject
    {
        public File[] Files { get; private set; }

        private Testimonial(File[] files)
        {
            Files = files;
        }

        public static Testimonial Create() => new([]);

        public Testimonial AddFile(File file)
        {
            File[] files = [.. Files, file];
            return new(files);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            foreach (var file in Files)
            {
                yield return file;
            }
        }
    }
}
