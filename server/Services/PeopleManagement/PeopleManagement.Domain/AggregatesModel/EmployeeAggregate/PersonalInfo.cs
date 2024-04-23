namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class PersonalInfo : ValueObject
    {        

        private MaritalStatus _maritalStatus = null!;
        private Gender _gender = null!;
        private Ethinicity _ethinicity = null!;
        private EducationLevel _educationLevel = null!;
        private Deficiency _deficiency = null!;


        public Deficiency Deficiency 
        {
            get => _deficiency; 
            private set
            {
                _deficiency = value;
            } 
        }
        public MaritalStatus MaritalStatus 
        {
            get => _maritalStatus;
            private set
            {
                _maritalStatus = value;
            }
        }       

        public Gender Gender
        {
            get => _gender;
            set
            {
                _gender = value;
            }
        }

        public Ethinicity Ethinicity
        {
            get => _ethinicity;
            set
            {
                _ethinicity = value;
            }
        }

        public EducationLevel EducationLevel
        {
            get => _educationLevel;
            set
            {
                _educationLevel = value;
            }
        }


        private PersonalInfo(Deficiency deficiency, MaritalStatus maritalStatus, Gender gender, Ethinicity ethinicity, EducationLevel educationLevel)
        {
            Deficiency = deficiency;
            MaritalStatus = maritalStatus;
            Gender = gender;
            Ethinicity = ethinicity;
            EducationLevel = educationLevel;
        }

        public static PersonalInfo Create(Deficiency deficiency, MaritalStatus maritalStatus, Gender gender, Ethinicity ethinicity, EducationLevel educationLevel)
            => new(deficiency, maritalStatus, gender, ethinicity, educationLevel);

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return MaritalStatus;
            yield return Gender;
            yield return Ethinicity;
            yield return EducationLevel;
            yield return Deficiency;
        }
    }
}
