namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class DocumentSigningOptions : Enumeration
    {
        public static readonly DocumentSigningOptions PhysicalSignature = new (1, nameof(PhysicalSignature));
        public static readonly DocumentSigningOptions DigitalSignatureAndWhatsapp = new (2, nameof(DigitalSignatureAndWhatsapp));
        public static readonly DocumentSigningOptions DigitalSignatureAndSelfie = new (3, nameof(DigitalSignatureAndSelfie));
        private DocumentSigningOptions(int id, string name) : base(id, name)
        {
        }

        public static implicit operator DocumentSigningOptions(int id) => Enumeration.FromValue<DocumentSigningOptions>(id);
        public static implicit operator DocumentSigningOptions(string name) => Enumeration.FromDisplayName<DocumentSigningOptions>(name);
    }
}
