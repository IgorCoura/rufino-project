using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockApi.Domain.Entities
{
    public class Product
    {
        public Product(Guid id, string name, string description, string section, string category, string unity, int quantity, DateTime modificationDate)
        {
            Id = id;
            Name = name;
            Description = description;
            Section = section;
            Category = category;
            Unity = unity;
            Quantity = quantity;
            ModificationDate = modificationDate;
        }

        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string Section { get; private set; }
        public string Category { get; private set; }
        public string Unity { get; private set; }
        public int Quantity { get; private set; } = 0;
        public DateTime ModificationDate { get; private set; }
    }
}
