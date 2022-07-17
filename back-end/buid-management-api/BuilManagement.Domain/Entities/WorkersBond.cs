using BuildManagement.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Entities
{
    public class WorkersBond: Entity
    {
        public DateTime InitBond { get; set; }
        public DateTime EndBond { get; set; }
        public Construction? Constrution { get; set; }
        public Guid ConstrutionId { get; set; }
        public Worker? Worker { get; set; }
        public Guid WorkerId { get; set; }
    }
}
