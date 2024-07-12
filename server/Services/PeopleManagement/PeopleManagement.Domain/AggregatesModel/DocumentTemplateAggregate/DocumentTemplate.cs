namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate
{
    public class DocumentTemplate(Guid id, Guid companyId, DirectoryName directory, FileName bodyFileName, FileName headerFileName, 
        FileName footerFileName, RecoverDataType recoverDataType, TimeSpan? documentValidityDuration) : Entity(id), IAggregateRoot
    {
        

        public Guid CompanyId { get; private set; } = companyId;
        public DirectoryName Directory { get; private set; } = directory;
        public FileName BodyFileName { get; private set; } = bodyFileName;
        public FileName HeaderFileName { get; private set; } = headerFileName;
        public FileName FooterFileName { get; private set; } = footerFileName;
        public RecoverDataType RecoverDataType { get; private set; } = recoverDataType;
        public TimeSpan? DocumentValidityDuration { get; private set; } = documentValidityDuration;

        public static DocumentTemplate Create(Guid id, Guid companyId, FileName bodyFileName, FileName headerFileName, 
            FileName footerFileName, RecoverDataType recoverDataType, TimeSpan? documentValidityDuration)
        {
            var directory = Guid.NewGuid().ToString();
            return new DocumentTemplate(id, companyId, directory, bodyFileName, headerFileName, footerFileName, recoverDataType, documentValidityDuration);
        }

        public static DocumentTemplate Create(Guid id, Guid companyId, DirectoryName directory, FileName bodyFileName, FileName headerFileName,
            FileName footerFileName, RecoverDataType recoverDataType, TimeSpan? documentValidityDuration)
        {
            return new DocumentTemplate(id, companyId, directory, bodyFileName, headerFileName, footerFileName, recoverDataType, documentValidityDuration);
        }
    }
}
