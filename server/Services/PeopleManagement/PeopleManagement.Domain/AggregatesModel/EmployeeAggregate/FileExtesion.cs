using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class FileExtesion : Enumeration
    {
        public static readonly FileExtesion PNG = new(1, nameof(PNG));        
        public static readonly FileExtesion JPEG = new(2, nameof(JPEG));
        public static readonly FileExtesion JPG = new(3, nameof(JPG));
        public static readonly FileExtesion PDF = new(4, nameof(PDF));

        public int Vlaue { get; set; } = 1199;
        private FileExtesion(int id, string name) : base(id, name)
        {
        }
    }
}
