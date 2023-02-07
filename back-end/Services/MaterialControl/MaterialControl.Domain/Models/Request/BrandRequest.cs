using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControl.Domain.Models.Request
{
    public record BrandRequest
    (
        Guid Id,
        string Name,
        string Description
    );
    
}
