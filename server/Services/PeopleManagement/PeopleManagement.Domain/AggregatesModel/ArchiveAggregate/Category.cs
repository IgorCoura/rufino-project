namespace PeopleManagement.Domain.AggregatesModel.ArchiveAggregate
{
    public sealed class Category : Enumeration
    {
        public static readonly Category Others = new(0, nameof(Others));
        public static readonly Category IdCard = new(1, nameof(IdCard));
        public static readonly Category VoteId = new(2, nameof(VoteId));
        public static readonly Category AddressProof = new (3, nameof(AddressProof));
        public static readonly Category Contract = new (4, nameof(Contract));
        public static readonly Category MarriageCertificate = new (5, nameof(MarriageCertificate));
        public static readonly Category ChildDocument = new (6, nameof(ChildDocument));
        public static readonly Category EducationalCertificate = new (7, nameof(EducationalCertificate));
        public static readonly Category VaccinationCertificate = new (8, nameof(VaccinationCertificate));
        public static readonly Category MilitaryDocument = new (9, nameof(MilitaryDocument));
        public static readonly Category MedicalAdmissionExam = new (10, nameof(MedicalAdmissionExam));
        public static readonly Category SpouseDocument = new (11, nameof(MedicalAdmissionExam));
        
        private Category(int id, string name) : base(id, name)
        {
        }
    }
}
