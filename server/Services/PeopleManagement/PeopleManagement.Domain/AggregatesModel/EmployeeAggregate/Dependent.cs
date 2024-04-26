using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class Dependent : Entity
    {
        public Name Name { get; private set; } = null!;
        public IdCard IdCard { get; private set; } = null!;
        public Gender Gender { get; private set; } = null!;
        public DependencyType DependencyType { get; private set; } = null!;
        public Testimonial Testimonial { get; private set; } = null!;

        private Dependent() { }

        private Dependent(Guid id, Name name, IdCard idCard, Gender gender, DependencyType dependencyType): base(id)
        {
            Name = name;
            IdCard = idCard;
            Gender = gender;
            DependencyType = dependencyType;
            Testimonial = Testimonial.Create();
        }

        public static Dependent Create(Guid id, Name name, IdCard idCard, Gender gender, DependencyType dependencyType) => new(id, name, idCard, gender, dependencyType);

        public void AddTestimonialFile(File file)
        {
            Testimonial = Testimonial.AddFile(file);
        }

        public void AddIdCardFile(File file)
        {
            IdCard = IdCard.AddFile(file);
        }

        public Result CheckPendingIssues()
        {
            var result = Result.Success(this.GetType().Name);

            result.AddResult(IdCard.CheckPendingIssues());
            result.AddResult(Testimonial.CheckPendingIssues());

            return result;
        }
    }
}
