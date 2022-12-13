using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Models.Material
{
    public record MaterialModel(
        Guid Id,
        string Name, 
        string Description, 
        string Unity, 
        decimal WorkHours
    );
}