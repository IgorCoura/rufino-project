using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate
{
    public class PeriodType : Enumeration
    {
        public static readonly PeriodType Daily = new(1, nameof(Daily));
        public static readonly PeriodType Weekly = new(2, nameof(Weekly));
        public static readonly PeriodType Monthly = new(3, nameof(Monthly));
        public static readonly PeriodType Yearly = new(4, nameof(Yearly));

        private PeriodType(int id, string name) : base(id, name)
        {
        }

        public static implicit operator PeriodType(int id) => CreateFromValue(id);
        public static implicit operator PeriodType(string name) => CreateFromDisplayName(name);

        public static PeriodType CreateFromDisplayName(string value)
        {
            try
            {
                value = value.Trim();
                return Enumeration.FromDisplayName<PeriodType>(value);
            }
            catch
            {
                throw new DomainException(nameof(PeriodType), DomainErrors.ErroCreateEnumeration(nameof(PeriodType), value));
            }
        }

        public static PeriodType CreateFromValue(int value)
        {
            try
            {
                return Enumeration.FromValue<PeriodType>(value);
            }
            catch
            {
                throw new DomainException(nameof(PeriodType), DomainErrors.ErroCreateEnumeration(nameof(PeriodType), value.ToString()));
            }
        }
    }
}
