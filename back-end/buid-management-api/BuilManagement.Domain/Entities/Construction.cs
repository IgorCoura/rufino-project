using BuildManagement.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Entities
{
    public class Construction: Entity
    {
        public string CorporateName { get; set; } = string.Empty;
        public string NickName { get; set; } = string.Empty;
        public Address? Address { get; set; }

    }
}
