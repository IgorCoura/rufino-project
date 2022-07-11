using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Entities
{
    public class MaterialItem
    {
        public Material? Material { get; set; }
        public int Quantity { get; set; }
        public Decimal Pricing { get; set; }
        public Decimal WorkHours { get; set; }

    }
}
