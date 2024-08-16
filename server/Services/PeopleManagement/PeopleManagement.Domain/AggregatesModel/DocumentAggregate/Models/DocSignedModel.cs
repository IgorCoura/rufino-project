namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Models
{
    public record DocSignedModel(Guid DocumentUnitId, Stream DocStream, Extension Extension);
}
