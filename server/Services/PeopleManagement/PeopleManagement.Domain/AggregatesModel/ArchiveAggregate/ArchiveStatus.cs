#pragma warning disable CS0618 // O arquivo INTEIRO e a feature Archive, descontinuada.
// Os tipos seguem marcados com [Obsolete] para quem estiver de fora; aqui dentro o aviso
// so produziria ruido no build de uma feature que ninguem deve mexer.
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;

namespace PeopleManagement.Domain.AggregatesModel.ArchiveAggregate
{
    public sealed class ArchiveStatus : Enumeration
    {
        public static readonly ArchiveStatus OK = new(1, nameof(OK));
        public static readonly ArchiveStatus RequiresFile = new (2, nameof(RequiresFile));
        public static readonly ArchiveStatus RequiresVerification = new (3, nameof(RequiresVerification));        

        private ArchiveStatus(int id, string name) : base(id, name)
        {
        }

        public static implicit operator ArchiveStatus(int id) => Enumeration.FromValue<ArchiveStatus>(id);
        public static implicit operator ArchiveStatus(string name) => Enumeration.FromDisplayName<ArchiveStatus>(name);
    }
}
