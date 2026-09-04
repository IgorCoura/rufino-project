namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options
{
    public class DocumentOptions
    {
        public const string ConfigurationSection = "Documents";

        public int WarningDaysBeforeDocumentExpiration { get; private set; } = 30;
        public double WarningRatio { get; private set; } = 0.3;
    }
}
