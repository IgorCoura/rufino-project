namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options
{
    public class DocumentTemplatesOptions
    {
        public const string ConfigurationSection = "DocumentTemplatesOptions";
        public string SourceDirectory { get;  set; } = "templates";
        public int MaxHoursWorkload { get;  set; } = 8;
    }
}
