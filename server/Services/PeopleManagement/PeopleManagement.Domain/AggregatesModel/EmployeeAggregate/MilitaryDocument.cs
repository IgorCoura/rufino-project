using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class MilitaryDocument : ValueObject
    {
        public const int MAX_NUMBER = 20;
        public const int MAX_TYPE = 50;
        public const int MIN_AGE = 19;
        public const int MAX_AGE = 45;

        private string _number = string.Empty;
        private string _type = string.Empty;
        private File[] _files = [];

        public string Number
        {
            get => _number;
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldNotBeNullOrEmpty(nameof(Number)));
                if (value.Length > MAX_NUMBER)
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldCannotBeLarger(nameof(Number), MAX_NUMBER));

                _number = value;
            }
        }
        public string Type 
        {
            get => _type;
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldNotBeNullOrEmpty(nameof(Type)));
                if (value.Length > MAX_TYPE)
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldCannotBeLarger(nameof(Type), MAX_TYPE));

                _type = value;
            }
        }

        public File[] Files
        {
            get => _files;
            private set
            {
                _files = value;
                _files = _files.Distinct().ToArray();
            }
        }



        private MilitaryDocument(string number, string type)
        {
            Number = number;
            Type = type;
        }

        private MilitaryDocument(string number, string type, File[] files)
        {
            Number = number;
            Type = type;
            Files = files;
        }


        public static MilitaryDocument Create(string number, string type) => new(number, type);
        public MilitaryDocument AddFile(File file)
        {
            File[] files = [.. Files, file];
            files = files.Distinct().ToArray();
            return new(Number, Type, files);
        }

        public static bool IsRequired(IdCard idCard, PersonalInfo personalInfo)
        {
            if(personalInfo.Gender != Gender.MALE)
                return false;
            if( idCard.Age() < MIN_AGE || idCard.Age() > MAX_AGE ) 
                return false;
            return true;
        }

        public bool HasValidFile => Files.Any(x => x.Valid);


        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Number;
            yield return Type;
        }
    }
}
