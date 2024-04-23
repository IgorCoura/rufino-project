using PeopleManagement.Domain.Exceptions;

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

        private Dependent(Guid id, string name, IdCard idCard, Gender gender, DependencyType dependencyType): base(id)
        {
            Name = name;
            IdCard = idCard;
            Gender = gender;
            DependencyType = dependencyType;
            Testimonial = Testimonial.Create();
        }

        public static Dependent Create(Guid id, string name, IdCard idCard, Gender gender, DependencyType dependencyType) => new(id, name, idCard, gender, dependencyType);

    }
}
