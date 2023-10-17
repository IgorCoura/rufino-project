namespace MaterialPurchase.Domain.Models.Request
{
    public record MaterialDraftPurchaseRequest
    {
        public MaterialDraftPurchaseRequest(Guid id, Guid materialId, Guid brandId, decimal unitPrice, double quantity)
        {
            Id = id;
            MaterialId = materialId;
            BrandId = brandId;
            UnitPrice = unitPrice;
            Quantity = quantity;
        }

        public Guid Id { get; set; }
        public Guid MaterialId { get; set; }
        public Guid BrandId { get; set; }
        public decimal UnitPrice { get; set; }
        public double Quantity { get; set; }
    }

}
