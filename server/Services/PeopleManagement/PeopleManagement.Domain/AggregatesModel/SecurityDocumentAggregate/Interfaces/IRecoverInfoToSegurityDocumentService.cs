namespace PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces
{
    public interface IRecoverInfoToSegurityDocumentService
    {
        Task<string> RecoverInfo();
    }
}
