namespace PeopleManagement.Domain.Options
{
    public class S3Options
    {
        public const string SectionName = "S3";

        public string ServiceURL { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public bool ForcePathStyle { get; set; } = true;
        public string AuthenticationRegion { get; set; } = string.Empty;
        public bool UseChunkEncoding { get; set; } = false;
        public bool AutoCloseStream { get; set; } = false;
    }
}
