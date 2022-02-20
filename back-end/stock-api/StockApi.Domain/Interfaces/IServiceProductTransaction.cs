using StockApi.Domain.Entities;
using StockApi.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockApi.Domain.Interfaces
{
    public interface IServiceProductTransaction
    {
        public Task<IEnumerable<ProductTransaction>> Insert(List<CreateTransactionModel> transactions);
    }
}
