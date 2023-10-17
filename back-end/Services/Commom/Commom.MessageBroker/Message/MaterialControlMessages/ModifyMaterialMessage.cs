using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.MessageBroker.Message.MaterialControlMessages
{
    public class ModifyMaterialMessage
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Unity { get; set; } = string.Empty;
    }
}
