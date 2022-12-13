using StockApi.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockApi.Domain.Interfaces
{
    public interface IServiceProduct
    {
        public IEnumerable<Product> RecoverAll(DateTime modificationDate);
        public IEnumerable<Product> RecoverAll();
    }
}
