namespace PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces
{
    public interface IRecoverInfoToSegurityDocumentService
    {
        Task<Dictionary<string, dynamic>> RecoverInfo();
    }
}
