using PeopleManagement.Domain.Exceptions;
namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class Deficiency : ValueObject
    {
        public const int MAX_OBSERVATION = 500;

        private Disability[] _disabilities = [];
        private string _observation = string.Empty;
        public Disability[] Disabilities 
        {
            get => _disabilities;
            private set
            {
                if (_disabilities.Length <= 0)
                    throw new DomainException(DomainErrors.ListNotBeNullOrEmpty(nameof(Disabilities)));
                _disabilities = value;
            }
        }
        public string Observation 
        {
            get => _observation;
            set
            {
                value = value.Trim();

                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException(DomainErrors.FieldNotBeNullOrEmpty(nameof(Observation)));

                if(value.Length > MAX_OBSERVATION)
                    throw new DomainException(DomainErrors.FieldCannotBeLarger(nameof(Observation), MAX_OBSERVATION));
                
                _observation = value;
            }
        
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            foreach (var disability in Disabilities)
            {
                yield return disability;
            }
            yield return Observation;

        }

        private Deficiency(Disability[] disabilities, string observation)
        {
            Disabilities = disabilities;
            Observation = observation;
        }

        public static Deficiency Create(string observation, params Disability[] disabilities) => new (disabilities, observation);
    }
}
