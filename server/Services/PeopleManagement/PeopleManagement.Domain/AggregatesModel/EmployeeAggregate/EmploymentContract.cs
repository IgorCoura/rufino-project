using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class EmploymentContract : ValueObject
    {
        public int MAX_RANGE_DATE_YEARS = 1;

        private DateOnly _initDate;
        private DateOnly? _finalDate = null;
        private EmploymentContactType _contactType = null!;
        private File[] _files = [];


        public DateOnly InitDate
        {
            get => _initDate;
            private set
            {

                var dateNow = DateOnly.FromDateTime(DateTime.UtcNow);
                var dateMax = dateNow.AddYears(MAX_RANGE_DATE_YEARS);
                var dateMin = dateNow.AddYears(MAX_RANGE_DATE_YEARS * -1);
                if (value < dateMin || value > dateMax)
                    throw new DomainException(this.GetType().Name, DomainErrors.DataHasBeBetween(nameof(InitDate), value, dateMin, dateMax));
                _initDate = dateNow;
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
                    var dateMax = dateNow.AddYears(MAX_RANGE_DATE_YEARS);
                    var dateMin = dateNow.AddYears(MAX_RANGE_DATE_YEARS * -1);
                    if (value < dateMin || value > dateMax)
                        throw new DomainException(this.GetType().Name, DomainErrors.DataHasBeBetween(nameof(FinalDate), (DateOnly)value, dateMin, dateMax));
                    _finalDate = dateNow;
                }
            }
        }
        public EmploymentContactType ContractType
        {
            get => _contactType;
            private set 
            {
                _contactType = value; 
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

        private EmploymentContract() { }
        private EmploymentContract(DateOnly initDate, EmploymentContactType contractType)
        {
            InitDate = initDate;
            ContractType = contractType;
        }
        private EmploymentContract(DateOnly initDate, DateOnly? finalDate, EmploymentContactType contractType, File[] files)
        {
            InitDate = initDate;
            FinalDate = finalDate;
            Files = files;
            ContractType = contractType;
        }

        public static EmploymentContract Create(DateOnly initDate, EmploymentContactType contractType) => new(initDate, contractType);
        public EmploymentContract FinshedContract(DateOnly finalDate) => new(InitDate, finalDate , ContractType, Files);

        public EmploymentContract AddFile(File file)
        {
            File[] files = [.. Files, file];
            return new(InitDate, FinalDate, ContractType, files);
        }
        public bool HasValidFile => Files.Any(x => x.Valid);

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return InitDate;
            yield return FinalDate;
            foreach (var file in Files)
            {
                yield return file;
            }
        }
    }
}
