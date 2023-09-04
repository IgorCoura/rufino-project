namespace MaterialPurchase.Domain.Models.Request
{
    public record ResourceEntity : ModelBase
    {
        public ResourceEntity(Guid id, Guid constructionId, Guid companyId) : base(constructionId, companyId)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }
}

