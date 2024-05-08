using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed class IdCard : ValueObject
    {
        public const int MAX_BIRTHCITY = 100;
        public const int MAX_BIRTHSTATE = 100;
        public const int MAX_NACIONALITY = 100;
        public const int MAX_AGE = 80;
        public const int MIN_AGE = 18;

        private CPF _cpf = null!;
        private Name _motherName = null!;
        private Name? _fatherName = null!;
        private string _birthcity = string.Empty;
        private string _birthstate = string.Empty;
        private string _nacionality = string.Empty;
        private DateOnly _dateOfBirth;

        public CPF Cpf 
        {
            get => _cpf;
            private set
            {
                _cpf = value;
            } 
        }
        
        public Name MotherName
        {
            get => _motherName;
            private set
            {
                _motherName = value;
            }
        }
        public Name? FatherName
        {
            get => _fatherName;
            private set
            {
                _fatherName = value;
            }
        }
        public string BirthCity
        {
            get => _birthcity;
            set
            {
                value = value.ToUpper().Trim();

                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldNotBeNullOrEmpty(nameof(BirthCity)));

                if (value.Length > MAX_BIRTHCITY)
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldCannotBeLarger(nameof(BirthCity), MAX_BIRTHCITY));

                _birthcity = value;
            }
        }

        public string BirthState
        {
            get => _birthstate;
            set
            {
                value = value.ToUpper().Trim();

                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldNotBeNullOrEmpty(nameof(BirthState)));

                if (value.Length > MAX_BIRTHSTATE)
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldCannotBeLarger(nameof(BirthState), MAX_BIRTHSTATE));

                _birthstate = value;
            }
        }

        public string Nacionality
        {
            get => _nacionality;
            set
            {
                value = value.ToUpper().Trim();

                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldNotBeNullOrEmpty(nameof(Nacionality)));

                if (value.Length > MAX_NACIONALITY)
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldCannotBeLarger(nameof(Nacionality), MAX_NACIONALITY));

                _nacionality = value;
            }
        }

        public DateOnly DateOfBirth
        {
            get => _dateOfBirth;
            set
            {
                var maxDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(MAX_AGE * -1);
                var minDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(MIN_AGE * -1);
                if (value < maxDate || value > minDate)
                    throw new DomainException(this.GetType().Name, DomainErrors.DataHasBeBetween(nameof(DateOfBirth), value, minDate, maxDate));
                _dateOfBirth = value;
            }
        }
        public IdCard(CPF cpf, Name motherName, Name? fatherName, string birthCity, string birthState, string nacionality, DateOnly dateOfBirth)
        {
            Cpf = cpf;
            MotherName = motherName;
            FatherName = fatherName;
            BirthCity = birthCity;
            BirthState = birthState;
            Nacionality = nacionality;
            DateOfBirth = dateOfBirth;
        }



        public static IdCard Create(CPF cpf, Name motherName, Name? fatherName, string birthCity, string birthState, string nacionality, DateOnly dateOfBirth)
            => new(cpf, motherName, fatherName, birthCity, birthState, nacionality, dateOfBirth);


        public int Age() 
        {
            var dateNow = DateOnly.FromDateTime(DateTime.UtcNow);
            var age = dateNow.Year - DateOfBirth.Year;
            if (DateOfBirth.Month > dateNow.Month)
                age -= 1;

            if (DateOfBirth.Month == dateNow.Month && DateOfBirth.Day > dateNow.Day)
                age -= 1;

            return age;
        }


        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Cpf;
            yield return MotherName;
            yield return FatherName;
            yield return BirthCity;
            yield return BirthState;                
            yield return Nacionality;
            yield return DateOfBirth;               
 
        }
    }
}
