namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate
{
    public class TemplateFileInfo : ValueObject
    {
        public DirectoryName Directory { get; private set; } = null!;
        public FileName BodyFileName { get; set; } = null!;
        public FileName HeaderFileName { get; set; } = null!;
        public FileName FooterFileName { get; set; } = null!;

        public List<RecoverDataType> RecoversDataType { get; set; } = null!;

        private TemplateFileInfo() { }

        private TemplateFileInfo(DirectoryName directory, FileName bodyFileName, FileName headerFileName,
            FileName footerFileName, List<RecoverDataType> recoversDataType)
        {
            Directory = directory;
            BodyFileName = bodyFileName;
            HeaderFileName = headerFileName;
            FooterFileName = footerFileName;
            RecoversDataType = recoversDataType;
        }

        public static TemplateFileInfo Create(
           DirectoryName directory,
           FileName bodyFileName,
           FileName headerFileName,
           FileName footerFileName,
           List<RecoverDataType> recoversDataType)
        {
            return new TemplateFileInfo(
                directory,
                bodyFileName,
                headerFileName,
                footerFileName,
                recoversDataType);
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Directory;
            yield return BodyFileName;
            yield return HeaderFileName;
            yield return FooterFileName;
            yield return RecoversDataType;
        }
    }
}
