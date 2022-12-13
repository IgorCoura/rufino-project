using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Models.Brand
{
    public record CreateBrandModel(
        string Name, 
        string Description
    );
}
