using BuildManagement.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Entities
{
    public class MaterialItem : Entity
    {
        public Material? Material { get; set; }
        public Guid MaterialId { get; set; }
        public int Quantity { get; set; }
        public Decimal Pricing { get; set; }
        public Decimal WorkHours { get; set; }
        public Job? Job { get; set; }
        public Guid JobId { get; set; }

    }
}
 