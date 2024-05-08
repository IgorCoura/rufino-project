namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed class Dependent : ValueObject
    {
        public Name Name { get; private set; } = null!;
        public IdCard IdCard { get; private set; } = null!;
        public Gender Gender { get; private set; } = null!;
        public DependencyType DependencyType { get; private set; } = null!;

        private Dependent() { }

        private Dependent( Name name, IdCard idCard, Gender gender, DependencyType dependencyType)
        {
            Name = name;
            IdCard = idCard;
            Gender = gender; 
            DependencyType = dependencyType;
        }

        public static Dependent Create(Name name, IdCard idCard, Gender gender, DependencyType dependencyType) 
        {
            var dependet = new Dependent(name, idCard, gender, dependencyType);
            return dependet;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Name;
            yield return IdCard;
            yield return Gender;
            yield return DependencyType;
        }
    }
}
