namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class ImageExtesion : Enumeration
    {
        public static readonly ImageExtesion PNG = new(1, nameof(PNG));
        public static readonly ImageExtesion JPEG = new(2, nameof(JPEG));
        public static readonly ImageExtesion JPG = new(3, nameof(JPG));
        private ImageExtesion(int id, string name) : base(id, name)
        {
        }
    }
}
