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
            this.date = date;
            QuantityVariation = quantityVariation;
        }

        public Guid Id { get; private set; }
        public Product? Product { get; private set; }
        public Guid ProductId { get; private set; }
        public Worker? Responsible { get; private set; }
        public Guid ResponsibleId { get; private set; }
        public Worker? Taker { get; private set; }
        public Guid TakerId { get; private set; }
        public DateTime date { get; private set; }
        public int QuantityVariation { get; private set; }

    }
}
