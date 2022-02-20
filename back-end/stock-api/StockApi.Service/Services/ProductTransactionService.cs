using StockApi.Domain.Entities;
using StockApi.Domain.Interfaces;
using StockApi.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockApi.Service.Services
{
    public class ProductTransactionService: IServiceProductTransaction
    {
        private readonly IRepositoryProductTransaction _repositoryTransaction;
        private readonly IRepositoryProduct _repositoryProduct;

        public ProductTransactionService(IRepositoryProductTransaction repositoryTransaction, IRepositoryProduct repositoryProduct)
        {
            _repositoryTransaction = repositoryTransaction;
            _repositoryProduct = repositoryProduct;
        }


        public async Task<IEnumerable<ProductTransaction>> Insert(List<CreateTransactionModel> transactions)
        {
            var orderTransactions = transactions.OrderBy(t => t.Date).ToArray();
            var resps = new List<ProductTransaction>();
            foreach (var transaction in orderTransactions)
            {
                if (_repositoryTransaction.Get(t => t.DeviceId == transaction.DeviceId) is not null)
                    continue;
                var entity = new ProductTransaction(Guid.NewGuid(),transaction.DeviceId, transaction.ProductId, transaction.ResponsibleId, transaction.TakerId, transaction.Date, transaction.QuantityVariation);
                await  ChangeProduct(entity);
                var resp = _repositoryTransaction.Save(entity);
                resps.Add(resp);
            }
            await _repositoryTransaction.UnitOfWork.SaveEntitiesAsync();
            return resps;
        }

        private async Task ChangeProduct(ProductTransaction transaction)
        {  
            var product = await _repositoryProduct.GetByIdAsync(transaction.ProductId);
            product.addQuantity(transaction.QuantityVariation);
            _repositoryProduct.Update(product);
        }
    }
}
