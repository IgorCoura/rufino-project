using Purchase.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Purchase.Domain.Entities
{
    public class Product: Entity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Sector Sector { get; set; }
        public Category Category { get; set; }
        public Unit Unit { get; set; }
        public List<decimal> Prices { get; set; }
    }
}
 