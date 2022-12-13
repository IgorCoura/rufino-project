using StockApi.Domain.Entities;
using StockApi.Domain.Exceptions;
using StockApi.Domain.Interfaces;
using StockApi.Domain.Seedwork;
using StockApi.Infra.Data.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockApi.Infra.Data.Repository
{
    public class ProductTransactionRepository : IRepositoryProductTransaction
    {
        private readonly StockApiContext _context;

        public IUnitOfWork UnitOfWork
        {
            get
            {
                return _context;
            }
        }

        public ProductTransactionRepository(StockApiContext context)
        {
            _context = context;
        }

        public ProductTransaction Save(ProductTransaction obj)
        {
            return _context.ProductTransaction.Add(obj).Entity;
        }
        public ProductTransaction Update(ProductTransaction obj)
        {
            return _context.ProductTransaction.Update(obj).Entity;

        }
        public ProductTransaction Remove(ProductTransaction obj)
        {
            return _context.ProductTransaction.Remove(obj).Entity;
        }

        public ProductTransaction GetByIdAsync(Guid id) =>
            _context.ProductTransaction.Where(p => p.Id == id).SingleOrDefault() ?? throw new DomainException("Product id is invalid");

        public ProductTransaction? Get(Func<ProductTransaction, bool> predicate) =>
            _context.ProductTransaction.Where(predicate).SingleOrDefault();


        public IEnumerable<ProductTransaction> GetAll(Func<ProductTransaction, bool> predicate) =>
             _context.ProductTransaction.Where(predicate);


    }
}
