#pragma warning disable CS0618 // O arquivo INTEIRO e a feature Archive, descontinuada.
// Os tipos seguem marcados com [Obsolete] para quem estiver de fora; aqui dentro o aviso
// so produziria ruido no build de uma feature que ninguem deve mexer.
namespace PeopleManagement.Application.Commands.ArchiveCommands.InsertFile
{
    [Obsolete("Feature Archive descontinuada em 2026-09-04: o desenvolvimento parou no meio e os endpoints foram removidos. Nao estenda nem use em codigo novo; ver o plano de refatoracao de autorizacao no CLAUDE.md.")]
    public record InsertFileCommand(Guid OwnerId, Guid CompanyId, Guid CategoryId, string FileExtesion, Stream stream) : IRequest<InsertFileResponse>
    {
    }

    public record InsertFileModel(Guid OwnerId, Guid CategoryId)
    {
        public InsertFileCommand ToCommand(Guid company, string FileExtesion, Stream stream) => new(OwnerId, company, CategoryId, FileExtesion, stream);
    }
}

