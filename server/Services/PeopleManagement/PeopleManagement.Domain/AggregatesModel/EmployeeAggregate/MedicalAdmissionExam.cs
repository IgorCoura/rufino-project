using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed class MedicalAdmissionExam : ValueObject
    {
        public const int DEFAULT_VALIDITY_EXAM_YEARS = 1;
        public const int MAX_YEARS_VALIDITY = 10;
        public const int LIMIT_DAYS_TO_DEMISSIONAL_EXAM = 90;

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
                    throw new DomainException(this.GetType().Name, DomainErrors.DataHasBeBetween(nameof(DateExam), value, dateMin, dateMax));
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
                if (value < dateNow || value > dateMax)
                    throw new DomainException(this.GetType().Name, DomainErrors.DataHasBeBetween(nameof(Validity), value, dateNow, dateMax));
                _validity = value;
            }
        }


        private MedicalAdmissionExam(DateOnly dateExam, DateOnly validity)
        {
            DateExam = dateExam;
            Validity = validity;
        }

        public static MedicalAdmissionExam Create(DateOnly dateExam, DateOnly validity) => new(dateExam, validity);

        public bool IsValid => _validity > DateOnly.FromDateTime(DateTime.UtcNow);

        public bool NeedDismissalExam => DateExam.AddDays(LIMIT_DAYS_TO_DEMISSIONAL_EXAM) > DateOnly.FromDateTime(DateTime.UtcNow);

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return DateExam;
            yield return Validity;
        }
    }
}
