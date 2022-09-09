using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Models.Material
{
    public record CreateMaterialModel(
        string Name,
        string Description,
        string Unity
    );
}
