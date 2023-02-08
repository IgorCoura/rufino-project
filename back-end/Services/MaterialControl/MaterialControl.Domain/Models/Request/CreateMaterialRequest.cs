using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControl.Domain.Models.Request
{
    public record CreateMaterialRequest
    (
        string Name,
        string Description,
        Guid UnityId
    );
}
