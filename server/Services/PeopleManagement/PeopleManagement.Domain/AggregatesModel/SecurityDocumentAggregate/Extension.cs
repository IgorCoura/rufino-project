using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate
{
    public class Extension : Enumeration
    {
        public static readonly Extension PDF = new(1, nameof(PDF));
        private Extension(int id, string name) : base(id, name)
        {
        }

        public static implicit operator Extension(int id)  => Enumeration.FromValue<Extension>(id);
        public static implicit operator Extension(string name)  => Enumeration.FromDisplayName<Extension>(name);

        public static Extension Create(string value)
        {
            try
            {
                value = value.Replace(".","").Trim().ToUpper();
                return Enumeration.FromDisplayName<Extension>(value);
            }
            catch
            {
                throw new DomainException(nameof(Extension), DomainErrors.ErroCreateEnumeration(nameof(Extension), value));
            }
        }

    }
}
