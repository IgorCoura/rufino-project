using PeopleManagement.Domain.Exceptions;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class MedicalExam : ValueObject
    {
        public const int DEFAULT_VALIDITY_EXAM_YEARS = 1;
        public const int MAX_YEARS_VALIDITY = 10;

        private DateOnly _dateExam;
        private DateOnly _validity;

        public DateOnly DateExam 
        {
            get => _dateExam;
            private set
            {
                var dateNow = DateOnly.FromDateTime(DateTime.UtcNow);
                var dateMin = dateNow.AddYears(DEFAULT_VALIDITY_EXAM_YEARS * -1);
                var dateMax = dateNow.AddDays(1);
                if (value <  dateMin || value > dateNow)
                    throw new DomainException(DomainErrors.DataHasBeBetween(nameof(DateExam), value, dateMin, dateMax));
                _dateExam = value;
            } 
        }

        public DateOnly Validity 
        {
            get => _validity;
            private set
            {
                var dateNow = DateOnly.FromDateTime(DateTime.UtcNow);
                var dateMax = dateNow.AddYears(MAX_YEARS_VALIDITY);
                if (_validity < dateNow || _validity > dateMax)
                    throw new DomainException(DomainErrors.DataHasBeBetween(nameof(Validity), value, dateNow, dateMax));
                _validity = value;
            }
        }


        private MedicalExam(DateOnly dateExam, DateOnly validity)
        {
            DateExam = dateExam;
            Validity = validity;
        }

        public static MedicalExam Create(DateOnly dateExam, DateOnly validity) => new(dateExam, validity);


        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return DateExam;
            yield return Validity;
        }
    }
}
