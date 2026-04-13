using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed class EmploymentContract : ValueObject
    {
        public int MAX_DATE_INIT_YEARS_MIN = 12;
        public int MAX_DATE_INIT_YEARS_MAX = 1;
        public int MAX_RANGE_DATE_FINAL_YEARS = 1;

        private DateOnly _initDate;
        private DateOnly? _finalDate = null;
        private EmploymentContractType _contractType = null!;

        public DateOnly InitDate
        {
            get => _initDate;
            private set
            {

                var dateNow = DateOnly.FromDateTime(DateTime.UtcNow);
                var dateMax = dateNow.AddYears(MAX_DATE_INIT_YEARS_MAX);
                var dateMin = dateNow.AddYears(MAX_DATE_INIT_YEARS_MIN * -1);
                if (value < dateMin || value > dateMax)
                    throw new DomainException(this.GetType().Name, DomainErrors.DataHasBeBetween(nameof(InitDate), value, dateMin, dateMax));
                _initDate = value;
            }
        }
        public DateOnly? FinalDate
        {
            get => _finalDate;
            private set
            {
                if (value != null)
                {
                    var dateNow = DateOnly.FromDateTime(DateTime.UtcNow);
                    var dateMax = dateNow.AddYears(MAX_RANGE_DATE_FINAL_YEARS);
                    var dateMin = dateNow.AddYears(MAX_RANGE_DATE_FINAL_YEARS * -1);
                    if (value < dateMin || value > dateMax)
                        throw new DomainException(this.GetType().Name, DomainErrors.DataHasBeBetween(nameof(FinalDate), (DateOnly)value, dateMin, dateMax));
                    _finalDate = value;
                }
            }
        }
        public EmploymentContractType ContractType
        {
            get => _contractType;
            private set 
            {
                _contractType = value; 
            }
        }      

        private EmploymentContract() { }
        private EmploymentContract(DateOnly initDate, EmploymentContractType contractType)
        {
            InitDate = initDate;
            ContractType = contractType;
        }
        private EmploymentContract(DateOnly initDate, DateOnly? finalDate, EmploymentContractType contractType)
        {
            InitDate = initDate;
            FinalDate = finalDate;
            ContractType = contractType;
        }

        public static EmploymentContract Create(DateOnly initDate, EmploymentContractType contractType) => new(initDate, contractType);
        public EmploymentContract FinshedContract(DateOnly finalDate) => new(InitDate, finalDate , ContractType);

        public bool IsActive => FinalDate == null || FinalDate > DateOnly.FromDateTime(DateTime.UtcNow);  
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return InitDate;
            yield return FinalDate;
        }
    }
}
