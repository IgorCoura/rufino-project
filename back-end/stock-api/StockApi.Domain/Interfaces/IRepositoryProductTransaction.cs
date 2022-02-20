using StockApi.Domain.Entities;
using StockApi.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockApi.Domain.Interfaces
{
    public interface IRepositoryProductTransaction: IRepository
    {
        public ProductTransaction Save(ProductTransaction obj);
        public ProductTransaction Update(ProductTransaction obj);
        public ProductTransaction Remove(ProductTransaction obj);
        public ProductTransaction GetByIdAsync(Guid id);
        public IEnumerable<ProductTransaction> GetAll(Func<ProductTransaction, bool> predicate);
        public ProductTransaction? Get(Func<ProductTransaction, bool> predicate);
    }
}
