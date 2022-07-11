using Purchase.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Purchase.Domain.Entities
{
    public class Provider: Entity
    {
        public string Name { get; set; }
        public string CorporateName { get; set; }
        public string CNPJ { get; set; }
        public Address Address { get; set; }

        public Provider(string name, string corporateName, string cNPJ, Address address)
        {
            Name = name;
            CorporateName = corporateName;
            CNPJ = cNPJ;
            Address = address;
        }
    }
}
