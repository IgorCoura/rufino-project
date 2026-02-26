namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class DocumentSigningOptions : Enumeration
    {
        public static readonly DocumentSigningOptions PhysicalSignature = new (1, nameof(PhysicalSignature));
        public static readonly DocumentSigningOptions DigitalSignatureAndWhatsapp = new (2, nameof(DigitalSignatureAndWhatsapp));
        public static readonly DocumentSigningOptions DigitalSignatureAndSelfie = new (3, nameof(DigitalSignatureAndSelfie));
        public static readonly DocumentSigningOptions DigitalSignatureAndSMS= new (4, nameof(DigitalSignatureAndSMS));
        public static readonly DocumentSigningOptions OnlySMS = new (5, nameof(OnlySMS));
        public static readonly DocumentSigningOptions OnlyWhatsapp = new (6, nameof(OnlyWhatsapp));

        private DocumentSigningOptions(int id, string name) : base(id, name)
        {
        }

        public static implicit operator DocumentSigningOptions(int id) => Enumeration.FromValue<DocumentSigningOptions>(id);
        public static implicit operator DocumentSigningOptions(string name) => Enumeration.FromDisplayName<DocumentSigningOptions>(name);
    }
}
