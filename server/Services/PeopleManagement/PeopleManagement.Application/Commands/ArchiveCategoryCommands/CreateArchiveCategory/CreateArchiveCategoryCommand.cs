#pragma warning disable CS0618 // O arquivo INTEIRO e a feature Archive, descontinuada.
// Os tipos seguem marcados com [Obsolete] para quem estiver de fora; aqui dentro o aviso
// so produziria ruido no build de uma feature que ninguem deve mexer.
using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate;

namespace PeopleManagement.Application.Commands.ArchiveCategoryCommands.CreateArchiveCategory
{
    [Obsolete("Feature Archive descontinuada em 2026-09-04: o desenvolvimento parou no meio e os endpoints foram removidos. Nao estenda nem use em codigo novo; ver o plano de refatoracao de autorizacao no CLAUDE.md.")]
    public record CreateArchiveCategoryCommand(string Name, string Description, int[] ListenEventsIds, Guid CompanyId) : IRequest<CreateArchiveCategoryResponse>
    {
        public ArchiveCategory ToArchiveCategory(Guid id) => ArchiveCategory.Create(id, Name, Description, [.. ListenEventsIds], CompanyId);
    }

    public record CreateArchiveCategoryModel(string Name, string Description, int[] ListenEventsIds)
    {
        public CreateArchiveCategoryCommand ToCommand(Guid company) => new(Name, Description, ListenEventsIds, company);
    }
}
