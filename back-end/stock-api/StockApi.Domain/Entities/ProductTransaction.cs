using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockApi.Domain.Entities
{
    public class ProductTransaction
    {
        public ProductTransaction(DateTime date, int quantityVariation)
        {
            Date = date;
            QuantityVariation = quantityVariation;
        }

        public ProductTransaction(Guid id, Guid deviceId, Guid productId, Guid responsibleId, Guid takerId, DateTime date, int quantityVariation)
        {
            Id = id;
            DeviceId = deviceId;
            ProductId = productId;
            ResponsibleId = responsibleId;
            TakerId = takerId;
            Date = date;
            QuantityVariation = quantityVariation;
        }

        public Guid Id { get; private set; }
        public Guid DeviceId { get; private set; }
        public Product? Product { get; private set; }
        public Guid ProductId { get; private set; }
        public Worker? Responsible { get; private set; }
        public Guid ResponsibleId { get; private set; }
        public Worker? Taker { get; private set; }
        public Guid TakerId { get; private set; }
        public DateTime Date { get; private set; }
        public int QuantityVariation { get; private set; }

    }
}
