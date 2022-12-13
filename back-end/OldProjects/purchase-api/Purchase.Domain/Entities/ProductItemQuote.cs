using Purchase.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Purchase.Domain.Entities
{
    public class ProductItemQuote: Entity
    {
        public Guid ProductId { get; set; }
        public Product Product { get; set; }
        public Guid ProductQuotesId { get; set; }
        public ProductQuotes ProductQuotes { get; set; }
        public Guid BrandId { get; set; }
        public Brand Brand { get; set; }
        public string Name { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public int Quantity { get; set; }
        public DateTime Date { get; set; }

    }
}
