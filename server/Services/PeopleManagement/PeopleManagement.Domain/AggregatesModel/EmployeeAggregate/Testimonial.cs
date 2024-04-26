using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

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
        public bool HasValidFile => Files.Any(x => x.Valid);

        public Result CheckPendingIssues()
        {
            var error = new List<Error>();

            if (!HasValidFile)
                error.Add(DomainErrors.FieldIsRequired(nameof(File)));

            return Result.Failure(this.GetType().Name, error);
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
