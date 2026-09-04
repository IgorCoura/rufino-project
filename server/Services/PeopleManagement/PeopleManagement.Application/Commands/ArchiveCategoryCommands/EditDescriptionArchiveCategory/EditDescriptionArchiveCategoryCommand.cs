#pragma warning disable CS0618 // O arquivo INTEIRO e a feature Archive, descontinuada.
// Os tipos seguem marcados com [Obsolete] para quem estiver de fora; aqui dentro o aviso
// so produziria ruido no build de uma feature que ninguem deve mexer.
using PeopleManagement.Application.Commands.ArchiveCategoryCommands.AddListenEvent;

namespace PeopleManagement.Application.Commands.ArchiveCategoryCommands.EditDescriptionArchiveCategory
{
    [Obsolete("Feature Archive descontinuada em 2026-09-04: o desenvolvimento parou no meio e os endpoints foram removidos. Nao estenda nem use em codigo novo; ver o plano de refatoracao de autorizacao no CLAUDE.md.")]
    public record EditDescriptionArchiveCategoryCommand(Guid ArchiveCategoryId, Guid CompanyId, String Description) : IRequest<EditDescriptionArchiveCategoryResponse>
    {
    }

    public record EditDescriptionArchiveCategoryModel(Guid ArchiveCategoryId, String Description)
    {
        public EditDescriptionArchiveCategoryCommand ToCommand(Guid company) => new(ArchiveCategoryId, company ,Description);
    }
}
