#pragma warning disable CS0618 // O arquivo INTEIRO e a feature Archive, descontinuada.
// Os tipos seguem marcados com [Obsolete] para quem estiver de fora; aqui dentro o aviso
// so produziria ruido no build de uma feature que ninguem deve mexer.
namespace PeopleManagement.Domain.AggregatesModel.ArchiveAggregate
{
    public sealed class FileStatus : Enumeration
    {
        public static readonly FileStatus Pending = new(1, nameof(Pending));
        public static readonly FileStatus OK = new(2, nameof(OK));
        public static readonly FileStatus Refused = new(3, nameof(Refused));
        public static readonly FileStatus NotApplicable = new(4, nameof(NotApplicable));
        private FileStatus(int id, string name) : base(id, name)
        {
        }

        public static implicit operator FileStatus(int id) => Enumeration.FromValue<FileStatus>(id);
        public static implicit operator FileStatus(string name) => Enumeration.FromDisplayName<FileStatus>(name);
    }
}
