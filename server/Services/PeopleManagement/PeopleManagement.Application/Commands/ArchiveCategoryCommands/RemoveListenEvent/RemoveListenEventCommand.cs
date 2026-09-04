#pragma warning disable CS0618 // O arquivo INTEIRO e a feature Archive, descontinuada.
// Os tipos seguem marcados com [Obsolete] para quem estiver de fora; aqui dentro o aviso
// so produziria ruido no build de uma feature que ninguem deve mexer.
namespace PeopleManagement.Application.Commands.ArchiveCategoryCommands.RemoveListenEvent
{
    [Obsolete("Feature Archive descontinuada em 2026-09-04: o desenvolvimento parou no meio e os endpoints foram removidos. Nao estenda nem use em codigo novo; ver o plano de refatoracao de autorizacao no CLAUDE.md.")]
    public record RemoveListenEventCommand(Guid ArchiveCategoryId, Guid CompanyId, int[] EventId) : IRequest<RemoveListenEventResponse>
    {
    }

    public record RemoveListenEventModel(Guid ArchiveCategoryId, int[] EventId)
    {
        public RemoveListenEventCommand ToCommand(Guid company) => new(ArchiveCategoryId, company, EventId);
    }
}
