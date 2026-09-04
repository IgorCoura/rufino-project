#pragma warning disable CS0618 // O arquivo INTEIRO e a feature Archive, descontinuada.
// Os tipos seguem marcados com [Obsolete] para quem estiver de fora; aqui dentro o aviso
// so produziria ruido no build de uma feature que ninguem deve mexer.
namespace PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.Interfaces
{
    [Obsolete("Feature Archive descontinuada em 2026-09-04: o desenvolvimento parou no meio e os endpoints foram removidos. Nao estenda nem use em codigo novo; ver o plano de refatoracao de autorizacao no CLAUDE.md.")]
    public interface IArchiveService
    {
        Task CreateFilesForEvent(Guid ownerId, Guid companyId, int eventId, CancellationToken cancellationToken = default);
        Task<Guid> InsertFile(Guid ownerId, Guid companyId, Guid categoryId, File file, Stream stream, CancellationToken cancellation = default);
        Task<bool> HasRequiresFiles(Guid ownerId, Guid companyId);
    }
}

