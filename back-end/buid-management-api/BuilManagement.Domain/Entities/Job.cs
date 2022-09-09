using BuildManagement.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Entities
{
    public class Job: Entity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Construction? Construction { get; set; }
        public Guid ConstructionId { get; set; }
        public List<MaterialItem> Material { get; set; } = new List<MaterialItem>();
        public List<WorkersBond> WorkersBonds { get; set; } = new List<WorkersBond>();
        public DateTime? InitJob { get; set; }
        public DateTime? EndJob { get; set; }
    }
}
