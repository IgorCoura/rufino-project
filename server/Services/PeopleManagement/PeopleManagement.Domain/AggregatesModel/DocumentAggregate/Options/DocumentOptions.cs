namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options
{
    public class DocumentOptions
    {
        public const string ConfigurationSection = "DocumentOptions";

        public int WarningDaysBeforeDocumentExpiration { get; private set; } = 30;
    }
}
