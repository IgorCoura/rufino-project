
namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class Extension : Enumeration
    {
        public static readonly Extension PNG = new(1, nameof(PNG));
        public static readonly Extension JPG = new(2, nameof(JPG));
        public static readonly Extension JPEG = new(3, nameof(JPEG));
        private Extension(int id, string name) : base(id, name)
        {
        }

        public static implicit operator Extension(int id) => Enumeration.FromValue<Extension>(id);
        public static implicit operator Extension(string name) => Enumeration.FromDisplayName<Extension>(name);

        
    }
}
