namespace PeopleManagement.Domain.Options
{
    public class LocalStorageOptions
    {
        public const string ConfigurationSection = "LocalStorageOptions";
        public string RootPath { get; set; } = "/root";
    }
}
