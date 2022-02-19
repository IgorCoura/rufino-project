using Microsoft.EntityFrameworkCore;
using StockApi.Domain.Seedwork;
using StockApi.Domain.Entities;
using StockApi.Domain.Exceptions;
using StockApi.Domain.Interfaces;
using StockApi.Infra.Data.Context;


namespace StockApi.Infra.Data.Repository
{
    public class ProductRepository: IRepositoryProduct
    {
        private readonly StockApiContext _context;

        public IUnitOfWork UnitOfWork
        {
            get
            {
                return _context;
            }
        }

        public ProductRepository(StockApiContext context)
        {
            _context = context;
        }

        public Product Save(Product obj)
        {
            return _context.Product.Add(obj).Entity;
        }
        public Product Update(Product obj)
        {
            return _context.Product.Update(obj).Entity;

        }
        public Product Remove(Product obj)
        {
            return _context.Product.Remove(obj).Entity;
        }

        public async Task<Product> GetByIdAsync(Guid id) =>
            await _context.Product.Where(p => p.Id == id).SingleOrDefaultAsync() ?? throw new DomainException("Product id is invalid");

        public IEnumerable<Product> GetAll(Func<Product, bool> predicate) =>
             _context.Product.Where(predicate);
    }
}
