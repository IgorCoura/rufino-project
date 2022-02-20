using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockApi.Domain.Entities
{
    public class Worker
    {
        public Worker(Guid id, string name, DateTime modificationDate)
        {
            Id = id;
            Name = name;
            ModificationDate = modificationDate;
        }

        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public DateTime ModificationDate { get; private set; }

    }
}
