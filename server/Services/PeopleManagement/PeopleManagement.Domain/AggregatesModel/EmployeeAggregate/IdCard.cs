using PeopleManagement.Domain.Exceptions;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class IdCard : ValueObject
    {
        public const int MAX_NUMBER = 20;
        public const int MAX_SHIPPING_ORGANIZATION = 60;
        public const int MAX_YEARS_CREATE_DATE = 15;

        private string _number = string.Empty;
        private CPF _cpf = null!;
        private DateOnly _createAt;
        private string _breedingOrganization = string.Empty;
        private Guid _archiveId = Guid.Empty;

        public string Number 
        { 
            get => _number;
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException(DomainErrors.FieldNotBeNullOrEmpty(nameof(Number)));
                if (value.Length > MAX_NUMBER)
                    throw new DomainException(DomainErrors.FieldCannotBeLarger(nameof(Number), MAX_NUMBER));
                _number = value;
            }
        }
        public CPF Cpf 
        {
            get => _cpf;
            private set
            {
                _cpf = value;
            } 
        }
        public DateOnly CreateAt 
        {
            get => _createAt;
            private set
            {
                var dateNow = DateOnly.FromDateTime(DateTime.Now);
                var dateMin = dateNow.AddYears(MAX_YEARS_CREATE_DATE * -1);
                if (_createAt > dateNow || dateMin < _createAt)
                    throw new DomainException(DomainErrors.DataHasBeBetween(nameof(CreateAt), value, dateMin, dateNow));
                _createAt = value;
            }
        }
        
        public string BreedingOrganization 
        { 
            get => _breedingOrganization;
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException(DomainErrors.FieldNotBeNullOrEmpty(nameof(Number)));
                if (value.Length > MAX_NUMBER)
                    throw new DomainException(DomainErrors.FieldCannotBeLarger(nameof(Number), MAX_NUMBER));
                _breedingOrganization = value;
            } 
        }
        public Guid ArchiveId
        {
            get => _archiveId;
            private set 
            {
                if (value != Guid.Empty)
                    throw new DomainException(DomainErrors.ObjectNotBeDefaultValue(nameof(ArchiveId), Guid.Empty.ToString()));
                _archiveId = value;
            } 
        }
        private IdCard(string number, CPF cpf, DateOnly shippingDate, string shippingOrganization, Guid archiveId)
        {
            Number = number;
            Cpf = cpf;
            CreateAt = shippingDate;
            BreedingOrganization = shippingOrganization;
            ArchiveId = archiveId;
        }

        public static IdCard Create(string number, CPF cpf, DateOnly shippingDate, string shippingOrganization, Guid archiveId)
        {
            return new IdCard(number, cpf, shippingDate, shippingOrganization, archiveId);   
        }


        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Number; 
            yield return Cpf; 
            yield return CreateAt; 
            yield return BreedingOrganization;
            yield return ArchiveId;
        }
    }
}
