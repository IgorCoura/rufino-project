using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Models.Provider
{
    public record ProviderModel(
        Guid Id,
        string Name, 
        string Description, 
        string Cnpj, 
        string Email, 
        string Site, 
        string Phone, 
        string Street, 
        string City, 
        string State, 
        string Country, 
        string ZipCode
    );
}
