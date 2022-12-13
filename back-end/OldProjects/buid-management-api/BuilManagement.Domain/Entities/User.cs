using BuildManagement.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Entities
{
    public class User : Entity
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
