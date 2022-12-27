using Commom.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Entities
{
    public class Provider : Entity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Cnpj { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Site { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public Address? Address { get; set; }
    }
}
