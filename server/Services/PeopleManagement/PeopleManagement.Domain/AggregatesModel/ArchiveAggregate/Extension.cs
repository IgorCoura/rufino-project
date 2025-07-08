using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;

namespace PeopleManagement.Domain.AggregatesModel.ArchiveAggregate
{
    public class Extension : Enumeration
    {
        public static readonly Extension PDF = new(1, nameof(PDF));
        public static readonly Extension PNG = new(2, nameof(PNG));
        public static readonly Extension JPEG = new(3, nameof(JPEG));
        public static readonly Extension JPG = new(4, nameof(JPG));
        public static readonly Extension SVG = new(5, nameof(SVG));
        public static readonly Extension TIFF = new(6, nameof(TIFF));
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
