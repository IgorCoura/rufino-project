using BuildManagement.Domain.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Models.Provider
{
    public class ProviderModel
    {

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public AddressModel? AddressModel { get; set; }
    }
}
