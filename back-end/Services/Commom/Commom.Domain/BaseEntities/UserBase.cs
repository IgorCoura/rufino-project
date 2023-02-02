using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.Domain.BaseEntities
{
    public class UserBase : Entity
    {
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public IEnumerable<string> FunctionsId { get; set; } = Enumerable.Empty<string>();
    }
}
