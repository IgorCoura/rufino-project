namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate
{
    public class TemplateFileInfo : ValueObject
    {
        public DirectoryName Directory { get; private set; } = null!;
        public FileName BodyFileName { get; set; } = null!;
        public FileName HeaderFileName { get; set; } = null!;
        public FileName FooterFileName { get; set; } = null!;
        public List<PlaceSignature> PlaceSignatures { get; set; } = [];
        public RecoverDataType RecoverDataType { get; set; } = null!;

        private TemplateFileInfo() { }

        private TemplateFileInfo(DirectoryName directory, FileName bodyFileName, FileName headerFileName,
            FileName footerFileName, List<PlaceSignature> placeSignatures, RecoverDataType recoverDataType)
        {
            Directory = directory;
            BodyFileName = bodyFileName;
            HeaderFileName = headerFileName;
            FooterFileName = footerFileName;
            PlaceSignatures = placeSignatures;
            RecoverDataType = recoverDataType;
        }

        public static TemplateFileInfo Create(
           DirectoryName directory,
           FileName bodyFileName,
           FileName headerFileName,
           FileName footerFileName,
           List<PlaceSignature> placeSignatures,
           RecoverDataType recoverDataType)
        {
            return new TemplateFileInfo(
                directory,
                bodyFileName,
                headerFileName,
                footerFileName,
                placeSignatures,
                recoverDataType);
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Directory;
            yield return BodyFileName;
            yield return HeaderFileName;
            yield return FooterFileName;
            yield return PlaceSignatures;
            yield return RecoverDataType;
        }
    }
}
