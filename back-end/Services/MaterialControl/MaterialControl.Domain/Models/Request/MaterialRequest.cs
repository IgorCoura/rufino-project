namespace MaterialControl.Domain.Models.Request
{
    public record MaterialRequest
    (
        Guid Id,
        string Name,
        string Description,
        Guid UnityId
    );
}
