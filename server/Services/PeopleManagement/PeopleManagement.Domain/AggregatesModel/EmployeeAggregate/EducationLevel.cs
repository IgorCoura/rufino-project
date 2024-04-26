namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class EducationLevel : Enumeration
    {
        public static readonly EducationLevel Illiterate = new(1, nameof(Illiterate));
        public static readonly EducationLevel IncompleteElementary = new(2, nameof(IncompleteElementary));
        public static readonly EducationLevel CompleteElementary = new(3, nameof(CompleteElementary));
        public static readonly EducationLevel IncompleteSecondary = new(4, nameof(IncompleteSecondary));
        public static readonly EducationLevel CompleteSecondary = new(5, nameof(CompleteSecondary));
        public static readonly EducationLevel IncompleteHigher = new(6, nameof(IncompleteHigher));
        public static readonly EducationLevel CompleteHigher = new(7, nameof(CompleteHigher));
        public static readonly EducationLevel CompletePostgraduate = new(8, nameof(CompletePostgraduate));
        public static readonly EducationLevel CompleteMasters = new(9, nameof(CompleteMasters));
        public static readonly EducationLevel CompleteDoctorate = new(10, nameof(CompleteDoctorate));

        private EducationLevel(int id, string name) : base(id, name)
        {
        }
    }
}
