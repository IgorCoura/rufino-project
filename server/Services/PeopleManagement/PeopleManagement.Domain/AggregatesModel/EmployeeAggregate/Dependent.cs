using PeopleManagement.Domain.Exceptions;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class Dependent : ValueObject
    {
        public Name Name { get; private set; }
        public IdCard IdCard { get; private set; }
        public PersonalInfo PersonalInfo { get; private set; }
        public Kinship Kinship { get; private set; }
        public Testimonial Testimonial { get; private set; }

        private Dependent(string name, IdCard idCard, PersonalInfo personalInfo, Kinship kinship, Testimonial testimonial)
        {
            Name = name;
            IdCard = idCard;
            PersonalInfo = personalInfo;
            Kinship = kinship;
            Testimonial = testimonial;
        }

        public static Dependent Create(string name, IdCard idCard, PersonalInfo personalInfo, Kinship kinship, Testimonial testimonial) => new(name, idCard, personalInfo, kinship, testimonial);

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Name;
            yield return IdCard;
            yield return PersonalInfo;
            yield return Kinship;
            yield return Testimonial;
        }
    }
}
