using StockApi.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockApi.Domain.Entities
{
    public class Worker : Entity
    {
        public Worker(Guid id,  string name, DateTime modificationDate): base(id)
        {

            Name = name;
            ModificationDate = modificationDate;
        }


        public string Name { get; private set; }
        public DateTime ModificationDate { get; private set; }

    }
}
