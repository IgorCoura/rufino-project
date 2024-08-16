using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate
{
    public class Extension : Enumeration
    {
        public static readonly Extension PDF = new(1, nameof(PDF));
        private Extension(int id, string name) : base(id, name)
        {
        }

        public static implicit operator Extension(int id)  => CreateFromValue(id);
        public static implicit operator Extension(string name)  => CreateFromDisplayName(name);

        public static Extension CreateFromDisplayName(string value)
        {
            try
            {
                value = value.Replace(".", "").Trim().ToUpper();
                return Enumeration.FromDisplayName<Extension>(value);
            }
            catch
            {
                throw new DomainException(nameof(Extension), DomainErrors.ErroCreateEnumeration(nameof(Extension), value));
            }
        }

        public static Extension CreateFromValue(int value)
        {
            try
            {
                return Enumeration.FromValue<Extension>(value);
            }
            catch
            {
                throw new DomainException(nameof(Extension), DomainErrors.ErroCreateEnumeration(nameof(Extension), value.ToString()));
            }
        }

    }
}
