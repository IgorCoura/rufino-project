using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed class Deficiency : ValueObject
    {
        public const int MAX_OBSERVATION = 500;

        private Disability[] _disabilities = [];
        private string _observation = string.Empty;
        public Disability[] Disabilities 
        {
            get => _disabilities;
            private set
            {
                _disabilities = value;
                _disabilities = _disabilities.Distinct().ToArray();
            }
        }
        public string Observation 
        {
            get => _observation;
            set
            {
                value = value.Trim();

                if(value.Length > MAX_OBSERVATION)
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldCannotBeLarger(nameof(Observation), MAX_OBSERVATION));
                
                _observation = value;
            }
        
        }
        private Deficiency(Disability[] disabilities, string observation)
        {
            Disabilities = disabilities;
            Observation = observation;
        }

        public static Deficiency Create(string observation, params Disability[] disabilities) => new (disabilities, observation);

        protected override IEnumerable<object> GetEqualityComponents()
        {
            foreach (var disability in Disabilities)
            {
                yield return disability;
            }
            yield return Observation;

        }
    }
}
