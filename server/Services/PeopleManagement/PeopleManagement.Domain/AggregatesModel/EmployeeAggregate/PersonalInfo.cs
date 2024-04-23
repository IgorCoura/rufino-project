using PeopleManagement.Domain.Exceptions;
using System.Globalization;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class PersonalInfo : ValueObject
    {
        public const int MAX_MOTHER_NAME = 100;
        public const int MAX_FATHER_NAME = 100;
        public const int MAX_BIRTHCITY = 100;
        public const int MAX_BIRTHSTATE = 100;
        public const int MAX_NACIONALITY = 100;
        public const int MAX_AGE = 80;
        public const int MIN_AGE = 18;

        private string? _motherName;
        private string? _fatherName;
        private MaritalStatus? _maritalStatus;
        private string? _birthcity;
        private string? _birthstate;
        private string? _nacionality;
        private DateOnly? _dateOfBirth;
        private Gender? _gender;

        public string MotherName 
        {
            get => _motherName ?? throw new ArgumentNullException();
            private set
            {
                value = value.ToUpper().Trim();

                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException(DomainErrors.FieldNotBeNullOrEmpty(nameof(MotherName)));

                if (value.Length > MAX_MOTHER_NAME)
                    throw new DomainException(DomainErrors.FieldCannotBeLarger(nameof(MotherName), MAX_MOTHER_NAME));

                _motherName = value;
            }
        }
        public string FatherName 
        { 
            get => _fatherName ?? throw new ArgumentNullException();
            private set
            {
                value = value.ToUpper().Trim();

                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException(DomainErrors.FieldNotBeNullOrEmpty(nameof(FatherName)));

                if (value.Length > MAX_FATHER_NAME)
                    throw new DomainException(DomainErrors.FieldCannotBeLarger(nameof(FatherName), MAX_FATHER_NAME));

                _fatherName = value;
            }
        }
        
        public MaritalStatus MaritalStatus 
        {
            get => _maritalStatus ?? throw new ArgumentNullException();
            private set
            {
                _maritalStatus = value;
            }
        }

        public string BirthCity
        {
            get => _birthcity ?? throw new ArgumentNullException();
            set
            {
                value = value.ToUpper().Trim();

                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException(DomainErrors.FieldNotBeNullOrEmpty(nameof(BirthCity)));

                if (value.Length > MAX_BIRTHCITY)
                    throw new DomainException(DomainErrors.FieldCannotBeLarger(nameof(BirthCity), MAX_BIRTHCITY));

                _birthcity = value;
            }
        }

        public string BirthState
        {
            get => _birthstate ?? throw new ArgumentNullException();
            set
            {
                value = value.ToUpper().Trim();

                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException(DomainErrors.FieldNotBeNullOrEmpty(nameof(BirthState)));

                if (value.Length > MAX_BIRTHSTATE)
                    throw new DomainException(DomainErrors.FieldCannotBeLarger(nameof(BirthState), MAX_BIRTHSTATE));

                _birthstate = value;
            }
        }

        public string Nacionality
        {
            get => _nacionality ?? throw new ArgumentNullException();
            set
            {
                value = value.ToUpper().Trim();

                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException(DomainErrors.FieldNotBeNullOrEmpty(nameof(Nacionality)));

                if (value.Length > MAX_NACIONALITY)
                    throw new DomainException(DomainErrors.FieldCannotBeLarger(nameof(Nacionality), MAX_NACIONALITY));

                _nacionality = value;
            }
        }

        public DateOnly DateOfBirth
        {
            get => _dateOfBirth ?? throw new ArgumentNullException();
            set
            {
                var maxDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(MAX_AGE * -1);
                var minDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(MIN_AGE * -1);
                if (value < maxDate || value > minDate)
                    throw new DomainException(DomainErrors.DataHasBeBetween(nameof(DateOfBirth),value, minDate ,maxDate));
                _dateOfBirth = value;
            }
        }

        public Gender Gender
        {
            get => _gender ?? throw new ArgumentNullException();
            set
            {
                _gender = value;
            }
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return MotherName;
            yield return FatherName;
            yield return MaritalStatus;
        }
    }
}
