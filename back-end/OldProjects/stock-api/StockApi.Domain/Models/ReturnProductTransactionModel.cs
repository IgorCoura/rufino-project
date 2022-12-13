using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockApi.Domain.Models
{
    public record ReturnProductTransactionModel(Guid DeviceId, Guid ServerId, DateTime date);
}
