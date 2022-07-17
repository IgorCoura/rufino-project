using BuildManagement.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Entities
{
    public class Worker: Entity
    {
        public decimal HourlyWage { get; set; }
        public string Office { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;

    }
}
