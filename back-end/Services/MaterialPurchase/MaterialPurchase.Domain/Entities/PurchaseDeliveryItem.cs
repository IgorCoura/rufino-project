using Commom.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Entities
{
    public class PurchaseDeliveryItem : Entity
    {
        public MaterialPurchase? MaterialPurchase { get; set; }
        public Guid MaterialPurchaseId { get; set; }
        public double Quantity { get; set; }
        public User? Receiver { get; set; }
        public Guid ReceiverId { get; set; }

    }
}
