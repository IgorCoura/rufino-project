using StockApi.Domain.Entities;
using StockApi.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockApi.Service.Services
{
    public class ProductService: IServiceProduct
    {
        private readonly IRepositoryProduct _repositoryProduct;

        public ProductService(IRepositoryProduct repositoryProduct)
        {
            _repositoryProduct = repositoryProduct;
        }

        public IEnumerable<Product> RecoverAll(DateTime modificationDate)
        {
            return _repositoryProduct.GetAll(p => p.ModificationDate > modificationDate);
        }
    }
}
