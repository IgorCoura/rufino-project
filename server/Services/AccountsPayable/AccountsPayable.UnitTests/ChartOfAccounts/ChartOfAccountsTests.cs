namespace AccountsPayable.UnitTests.ChartOfAccounts;

using AccountsPayable.Domain.ChartOfAccounts;
using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.ChartOfAccounts.Enumerations;
using AccountsPayable.Domain.ChartOfAccounts.Events;
using AccountsPayable.Domain.ChartOfAccounts.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.UnitTests.ChartOfAccounts.Mothers;

public class ChartOfAccountsTests
{
    private static readonly DateTime FIXED_NOW = ChartOfAccountsMother.DEFAULT_OCCURRED_AT;
    private static readonly DateTime LATER = FIXED_NOW.AddMinutes(5);

    public class WhenCreating
    {
        // Create monta ChartOfAccounts vazio (sem accounts), preserva nome/tenant e emite ChartOfAccountsCreated.
        [Fact]
        public void Create_WithValidName_ShouldStartEmptyAndEmitEvent()
        {
            var chart = ChartOfAccountsMother.Empty();

            Assert.Empty(chart.Accounts);
            Assert.Equal("Plano Padrão", chart.Name.Value);
            Assert.Equal(ChartOfAccountsMother.DEFAULT_TENANT, chart.TenantId);
            var created = Assert.IsType<ChartOfAccountsCreated>(chart.PullDomainEvents().Single());
            Assert.Equal(chart.Id, created.ChartOfAccountsId);
        }
    }

    public class WhenAddingAccount
    {
        // AddAccount root (parentId=null) inclui na lista, marca como ativo e emite AccountAdded.
        [Fact]
        public void AddAccount_AsRoot_ShouldAppendActiveAccountAndEmitEvent()
        {
            var chart = ChartOfAccountsMother.Empty();
            chart.PullDomainEvents();
            var id = AccountId.New();

            var account = chart.AddAccount(id, parentId: null,
                code: new AccountCode("4"),
                name: new AccountName("Despesas"),
                type: AccountType.Expense,
                occurredAt: LATER);

            Assert.True(account.IsActive);
            Assert.Null(account.ParentId);
            Assert.Same(account, chart.Accounts.Single());
            var added = Assert.IsType<AccountAdded>(chart.PullDomainEvents().Single());
            Assert.Null(added.ParentId);
            Assert.Equal("4", added.Code);
            Assert.Equal("EXPENSE", added.Type);
        }

        // AddAccount como filho de uma conta existente registra ParentId e emite o evento com ParentId no payload.
        [Fact]
        public void AddAccount_AsChild_ShouldReferenceParentInEntityAndEvent()
        {
            var chart = ChartOfAccountsMother.Empty();
            var rootId = AccountId.New();
            chart.AddAccount(rootId, null, new AccountCode("4"), new AccountName("Despesas"), AccountType.Expense, FIXED_NOW);
            chart.PullDomainEvents();
            var childId = AccountId.New();

            chart.AddAccount(childId, parentId: rootId,
                code: new AccountCode("4.01"),
                name: new AccountName("Operacionais"),
                type: AccountType.Expense,
                occurredAt: LATER);

            var child = chart.Accounts.Single(a => a.Id.Equals(childId));
            Assert.Equal(rootId, child.ParentId);
            var added = Assert.IsType<AccountAdded>(chart.PullDomainEvents().Single());
            Assert.Equal(rootId, added.ParentId);
        }

        // Adicionar conta com Code duplicado lança AP.COA02 — código é único no plano.
        [Fact]
        public void AddAccount_WithDuplicatedCode_ShouldThrowDomainException()
        {
            var chart = ChartOfAccountsMother.Empty();
            chart.AddAccount(AccountId.New(), null, new AccountCode("4"), new AccountName("Despesas"), AccountType.Expense, FIXED_NOW);

            var ex = Assert.Throws<DomainException>(() => chart.AddAccount(
                AccountId.New(), null, new AccountCode("4"), new AccountName("Outra"), AccountType.Asset, LATER));

            Assert.Equal("AP.COA02", ex.Id);
        }

        // Adicionar filho com ParentId inexistente lança AP.COA03.
        [Fact]
        public void AddAccount_WithUnknownParentId_ShouldThrowDomainException()
        {
            var chart = ChartOfAccountsMother.Empty();
            var unknownParent = AccountId.New();

            var ex = Assert.Throws<DomainException>(() => chart.AddAccount(
                AccountId.New(), parentId: unknownParent,
                new AccountCode("4.01"), new AccountName("Conta"), AccountType.Expense, LATER));

            Assert.Equal("AP.COA03", ex.Id);
        }

        // Adicionar filho de uma conta inativa lança AP.COA04.
        [Fact]
        public void AddAccount_UnderInactiveParent_ShouldThrowDomainException()
        {
            var (chart, rootId, childId) = ChartOfAccountsMother.WithRootAndOneChild();
            chart.DeactivateAccount(childId, LATER);
            chart.DeactivateAccount(rootId, LATER.AddMinutes(1));

            var ex = Assert.Throws<DomainException>(() => chart.AddAccount(
                AccountId.New(), parentId: rootId,
                new AccountCode("4.99"), new AccountName("Nova"), AccountType.Expense, LATER.AddMinutes(2)));

            Assert.Equal("AP.COA04", ex.Id);
        }

        // Adicionar conta abaixo de Account.MAX_DEPTH é permitido (limite exato é aceito).
        [Fact]
        public void AddAccount_AtExactMaxDepth_ShouldSucceed()
        {
            var (chart, _) = ChartOfAccountsMother.ChainOfDepth(Account.MAX_DEPTH);

            Assert.Equal(Account.MAX_DEPTH, chart.Accounts.Count);
        }

        // Tentar criar uma conta filha de um nó já no MAX_DEPTH (atingindo profundidade MAX_DEPTH+1) lança AP.COA05.
        [Fact]
        public void AddAccount_ExceedingMaxDepth_ShouldThrowDomainException()
        {
            var (chart, deepestId) = ChartOfAccountsMother.ChainOfDepth(Account.MAX_DEPTH);

            var ex = Assert.Throws<DomainException>(() => chart.AddAccount(
                AccountId.New(), parentId: deepestId,
                new AccountCode("4.1.1.1.1.1"), new AccountName("Profundo demais"), AccountType.Expense, LATER));

            Assert.Equal("AP.COA05", ex.Id);
        }
    }

    public class WhenRenamingAccount
    {
        // RenameAccount muda o Name da conta e emite AccountRenamed com oldName/newName.
        [Fact]
        public void RenameAccount_WithDifferentName_ShouldUpdateAndEmitEvent()
        {
            var (chart, _, childId) = ChartOfAccountsMother.WithRootAndOneChild();
            chart.PullDomainEvents();

            chart.RenameAccount(childId, new AccountName("Despesas administrativas"), LATER);

            var renamed = Assert.IsType<AccountRenamed>(chart.PullDomainEvents().Single());
            Assert.Equal("Despesas operacionais", renamed.OldName);
            Assert.Equal("Despesas administrativas", renamed.NewName);
            Assert.Equal("Despesas administrativas", chart.Accounts.Single(a => a.Id.Equals(childId)).Name.Value);
        }

        // Renomear para o mesmo nome atual é no-op (idempotente).
        [Fact]
        public void RenameAccount_WithSameName_ShouldBeNoOp()
        {
            var (chart, _, childId) = ChartOfAccountsMother.WithRootAndOneChild();
            chart.PullDomainEvents();

            chart.RenameAccount(childId, new AccountName("Despesas operacionais"), LATER);

            Assert.Empty(chart.PullDomainEvents());
        }

        // RenameAccount com Id desconhecido lança AP.COA06.
        [Fact]
        public void RenameAccount_WithUnknownId_ShouldThrowDomainException()
        {
            var chart = ChartOfAccountsMother.Empty();

            var ex = Assert.Throws<DomainException>(() => chart.RenameAccount(
                AccountId.New(), new AccountName("Conta"), LATER));

            Assert.Equal("AP.COA06", ex.Id);
        }
    }

    public class WhenDeactivatingAccount
    {
        // DeactivateAccount em folha (sem filhos) marca IsActive=false e emite AccountDeactivated.
        [Fact]
        public void DeactivateAccount_OnLeaf_ShouldDeactivateAndEmitEvent()
        {
            var (chart, _, childId) = ChartOfAccountsMother.WithRootAndOneChild();
            chart.PullDomainEvents();

            chart.DeactivateAccount(childId, LATER);

            var account = chart.Accounts.Single(a => a.Id.Equals(childId));
            Assert.False(account.IsActive);
            var deactivated = Assert.IsType<AccountDeactivated>(chart.PullDomainEvents().Single());
            Assert.Equal(childId, deactivated.AccountId);
        }

        // Desativar conta-pai enquanto há filhos ativos lança AP.COA08.
        [Fact]
        public void DeactivateAccount_WithActiveChildren_ShouldThrowDomainException()
        {
            var (chart, rootId, _) = ChartOfAccountsMother.WithRootAndOneChild();

            var ex = Assert.Throws<DomainException>(() => chart.DeactivateAccount(rootId, LATER));

            Assert.Equal("AP.COA08", ex.Id);
        }

        // Após desativar filhos, desativar o pai funciona.
        [Fact]
        public void DeactivateAccount_AfterChildrenDeactivated_ShouldSucceed()
        {
            var (chart, rootId, childId) = ChartOfAccountsMother.WithRootAndOneChild();
            chart.DeactivateAccount(childId, LATER);
            chart.PullDomainEvents();

            chart.DeactivateAccount(rootId, LATER.AddMinutes(1));

            Assert.False(chart.Accounts.Single(a => a.Id.Equals(rootId)).IsActive);
        }

        // Desativar conta já inativa lança AP.COA07.
        [Fact]
        public void DeactivateAccount_AlreadyInactive_ShouldThrowDomainException()
        {
            var (chart, _, childId) = ChartOfAccountsMother.WithRootAndOneChild();
            chart.DeactivateAccount(childId, LATER);

            var ex = Assert.Throws<DomainException>(() => chart.DeactivateAccount(childId, LATER.AddMinutes(1)));

            Assert.Equal("AP.COA07", ex.Id);
        }

        // Desativar Id desconhecido lança AP.COA06.
        [Fact]
        public void DeactivateAccount_WithUnknownId_ShouldThrowDomainException()
        {
            var chart = ChartOfAccountsMother.Empty();

            var ex = Assert.Throws<DomainException>(() => chart.DeactivateAccount(AccountId.New(), LATER));

            Assert.Equal("AP.COA06", ex.Id);
        }
    }
}
