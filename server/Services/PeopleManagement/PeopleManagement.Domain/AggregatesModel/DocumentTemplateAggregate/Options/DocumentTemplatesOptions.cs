﻿namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options
{
    public class DocumentTemplatesOptions
    {
        public const string ConfigurationSection = "DocumentTemplatesOptions";
        public string SourceDirectory { get; private set; } = "templates";
        public int MaxHoursWorkload { get; private set; } = 8;
    }
}
