using StockApi.Domain.Entities;
using StockApi.Domain.SeedWork;

namespace StockApi.Domain.Interfaces
{
    public interface IRepositoryProduct: IRepository
    {
        public Product Save(Product obj);
        public Product Update(Product obj);
        public Product Remove(Product obj);   
        public Task<Product> GetByIdAsync(Guid id);
        public IEnumerable<Product> GetAll(Func<Product, bool> predicate);
    }
}
