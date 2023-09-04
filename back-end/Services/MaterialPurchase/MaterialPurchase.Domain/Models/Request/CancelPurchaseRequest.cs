namespace MaterialPurchase.Domain.Models.Request
{
    public record CancelPurchaseRequest : ModelBase
    {
        public CancelPurchaseRequest(Guid constructionId, Guid companyId, Guid purchaseId, string comment) : base(constructionId, companyId)
        {
            ConstructionId = constructionId;
            PurchaseId = purchaseId;
            Comment = comment;
        }

        public Guid PurchaseId { get; set; }
        public string Comment { get; set; }
    }
}
