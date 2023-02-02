using Commom.Domain.BaseEntities;

namespace MaterialPurchase.Domain.BaseEntities
{
    public class PurchaseDeliveryItem : Entity
    {
        public  Purchase? Purchase { get; set; }
        public  Guid PurchaseId { get; set; }
        public ItemMaterialPurchase? MaterialPurchase { get; set; }
        public Guid MaterialPurchaseId { get; set; }
        public double Quantity { get; set; }
        public User? Receiver { get; set; }
        public Guid ReceiverId { get; set; }

    }
}
