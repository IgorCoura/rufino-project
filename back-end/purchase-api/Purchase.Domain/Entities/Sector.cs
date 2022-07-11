using Purchase.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Purchase.Domain.Entities
{
    public class Sector: Entity
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
