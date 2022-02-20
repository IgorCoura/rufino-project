using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockApi.Domain.Models
{
    public record CreateTransactionModel(Guid DeviceId, Guid ProductId, Guid ResponsibleId, Guid TakerId, DateTime Date, int QuantityVariation);
    
}
