namespace AccountsPayable.UnitTests.Suppliers;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;
using AccountsPayable.Domain.Suppliers.Enumerations;
using AccountsPayable.Domain.Suppliers.Events;
using AccountsPayable.Domain.Suppliers.ValueObjects;
using AccountsPayable.UnitTests.Suppliers.Mothers;

public class SupplierTests
{
    private static readonly DateTime FIXED_NOW = new(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime LATER = FIXED_NOW.AddMinutes(5);

    public class WhenCreating
    {
        // Create monta o Supplier no estado Active, preserva todos os VOs e timestamps.
        [Fact]
        public void Create_WithValidData_ShouldReturnSupplierInActiveStatus()
        {
            var supplier = SupplierMother.Active();

            Assert.Equal(SupplierStatus.Active, supplier.Status);
            Assert.Equal("Acme Brasil LTDA", supplier.LegalName.Value);
            Assert.Equal("Acme", supplier.TradeName?.Value);
            Assert.Equal("59199597000198", supplier.TaxId.Value);
            Assert.Equal("contato@acme.com.br", supplier.Contact.Email.Value);
            Assert.Equal(SupplierMother.DEFAULT_OCCURRED_AT, supplier.CreatedAt);
            Assert.Equal(SupplierMother.DEFAULT_OCCURRED_AT, supplier.UpdatedAt);
        }

        // Create emite SupplierCreated com o payload completo (incluindo tipo do TaxId em string).
        [Fact]
        public void Create_ShouldEmitSupplierCreatedEvent()
        {
            var supplier = SupplierMother.Active();

            var events = supplier.PullDomainEvents();
            var created = Assert.Single(events);
            var e = Assert.IsType<SupplierCreated>(created);
            Assert.Equal(supplier.Id, e.SupplierId);
            Assert.Equal(SupplierMother.DEFAULT_TENANT, e.TenantId);
            Assert.Equal("Acme Brasil LTDA", e.LegalName);
            Assert.Equal("Acme", e.TradeName);
            Assert.Equal("59199597000198", e.TaxIdValue);
            Assert.Equal("CNPJ", e.TaxIdType);
            Assert.Equal(SupplierMother.DEFAULT_OCCURRED_AT, e.OccurredAt);
        }

        // Create sem TradeName aceita null e emite evento com TradeName null no payload.
        [Fact]
        public void Create_WithoutTradeName_ShouldStoreNullAndEmitNullInEvent()
        {
            var supplier = SupplierMother.Active(tradeName: null);

            Assert.Null(supplier.TradeName);
            var e = Assert.IsType<SupplierCreated>(supplier.PullDomainEvents()[0]);
            Assert.Null(e.TradeName);
        }
    }

    public class WhenRenaming
    {
        // Rename muda LegalName, atualiza UpdatedAt e emite SupplierRenamed com nome antigo e novo.
        [Fact]
        public void Rename_WithDifferentName_ShouldUpdateLegalNameAndEmitEvent()
        {
            var supplier = SupplierMother.Active();
            supplier.PullDomainEvents();

            supplier.Rename(new LegalName("Acme Reformulada LTDA"), LATER);

            Assert.Equal("Acme Reformulada LTDA", supplier.LegalName.Value);
            Assert.Equal(LATER, supplier.UpdatedAt);
            var renamed = Assert.IsType<SupplierRenamed>(supplier.PullDomainEvents()[0]);
            Assert.Equal("Acme Brasil LTDA", renamed.OldLegalName);
            Assert.Equal("Acme Reformulada LTDA", renamed.NewLegalName);
        }

        // Rename com o mesmo nome atual é idempotente: não emite evento nem altera UpdatedAt.
        [Fact]
        public void Rename_WithSameName_ShouldBeNoOp()
        {
            var supplier = SupplierMother.Active();
            supplier.PullDomainEvents();
            var prevUpdatedAt = supplier.UpdatedAt;

            supplier.Rename(new LegalName("Acme Brasil LTDA"), LATER);

            Assert.Empty(supplier.PullDomainEvents());
            Assert.Equal(prevUpdatedAt, supplier.UpdatedAt);
        }
    }

    public class WhenUpdatingContact
    {
        // UpdateContact substitui o contato, atualiza UpdatedAt e emite SupplierContactUpdated com novo email/phone.
        [Fact]
        public void UpdateContact_WithDifferentContact_ShouldReplaceAndEmitEvent()
        {
            var supplier = SupplierMother.Active();
            supplier.PullDomainEvents();
            var newContact = new ContactInfo(
                new EmailAddress("novo@acme.com.br"),
                new PhoneNumber("11987654321"));

            supplier.UpdateContact(newContact, LATER);

            Assert.Equal("novo@acme.com.br", supplier.Contact.Email.Value);
            Assert.Equal(LATER, supplier.UpdatedAt);
            var updated = Assert.IsType<SupplierContactUpdated>(supplier.PullDomainEvents()[0]);
            Assert.Equal("novo@acme.com.br", updated.Email);
            Assert.Equal("11987654321", updated.Phone);
        }

        // UpdateContact com contato igual ao atual é idempotente: sem evento, sem UpdatedAt novo.
        [Fact]
        public void UpdateContact_WithSameContact_ShouldBeNoOp()
        {
            var supplier = SupplierMother.Active();
            supplier.PullDomainEvents();

            supplier.UpdateContact(new ContactInfo(new EmailAddress("contato@acme.com.br")), LATER);

            Assert.Empty(supplier.PullDomainEvents());
        }
    }

    public class WhenAddingBankAccount
    {
        // AddBankAccount(BankTransfer) adiciona o VO em ActiveBankAccounts e emite evento com Variant=BANK_TRANSFER.
        [Fact]
        public void AddBankAccount_BankTransfer_ShouldAddAndEmitEventWithBankTransferPayload()
        {
            var supplier = SupplierMother.Active();
            supplier.PullDomainEvents();
            var account = SupplierMother.BankTransferAccount();

            supplier.AddBankAccount(account, LATER);

            Assert.Equal(account, supplier.ActiveBankAccounts.Single());
            var added = Assert.IsType<SupplierBankAccountAdded>(supplier.PullDomainEvents()[0]);
            Assert.Equal("BANK_TRANSFER", added.Variant);
            Assert.Equal("001", added.BankCode);
            Assert.Equal("0001", added.Branch);
            Assert.Equal("123456-7", added.AccountNumber);
            Assert.Equal("CHECKING", added.AccountType);
            Assert.Null(added.PixKeyValue);
            Assert.Null(added.PixKeyType);
        }

        // AddBankAccount(Pix) adiciona o VO em ActiveBankAccounts e emite evento com Variant=PIX e os campos bancários nulos.
        [Fact]
        public void AddBankAccount_Pix_ShouldAddAndEmitEventWithPixPayload()
        {
            var supplier = SupplierMother.Active();
            supplier.PullDomainEvents();
            var account = SupplierMother.PixAccount();

            supplier.AddBankAccount(account, LATER);

            Assert.Equal(account, supplier.ActiveBankAccounts.Single());
            var added = Assert.IsType<SupplierBankAccountAdded>(supplier.PullDomainEvents()[0]);
            Assert.Equal("PIX", added.Variant);
            Assert.Equal("59199597000198", added.PixKeyValue);
            Assert.Equal("CNPJ", added.PixKeyType);
            Assert.Null(added.BankCode);
            Assert.Null(added.Branch);
            Assert.Null(added.AccountNumber);
            Assert.Null(added.AccountType);
        }

        // Adicionar VO equivalente (igualdade estrutural) a uma conta já em ActiveBankAccounts lança AP.SUP05.
        [Fact]
        public void AddBankAccount_WithDuplicateActive_ShouldThrowDomainException()
        {
            var supplier = SupplierMother.ActiveWithBankAccount();
            var duplicate = SupplierMother.BankTransferAccount();

            var ex = Assert.Throws<DomainException>(() => supplier.AddBankAccount(duplicate, LATER));

            Assert.Equal("AP.SUP05", ex.Id);
        }

        // Duplicação também vale para PixAccount com a mesma chave.
        [Fact]
        public void AddBankAccount_WithDuplicatePix_ShouldThrowDomainException()
        {
            var supplier = SupplierMother.ActiveWithPixAccount();
            var duplicate = SupplierMother.PixAccount();

            var ex = Assert.Throws<DomainException>(() => supplier.AddBankAccount(duplicate, LATER));

            Assert.Equal("AP.SUP05", ex.Id);
        }
    }

    public class WhenDeactivatingBankAccount
    {
        // Desativar um VO que não está nas contas ativas lança AP.SUP04.
        [Fact]
        public void DeactivateBankAccount_WithUnknownAccount_ShouldThrowDomainException()
        {
            var supplier = SupplierMother.ActiveWithBankAccount();
            var unknown = SupplierMother.BankTransferAccount(accountNumber: "999999-9");

            var ex = Assert.Throws<DomainException>(() => supplier.DeactivateBankAccount(unknown, LATER));

            Assert.Equal("AP.SUP04", ex.Id);
        }

        // Desativar a única conta ativa de um Supplier Active lança AP.SUP03 (invariante "fornecedor ativo precisa de conta").
        [Fact]
        public void DeactivateBankAccount_WhenLastAccountAndActive_ShouldThrowDomainException()
        {
            var supplier = SupplierMother.ActiveWithBankAccount();
            var account = supplier.ActiveBankAccounts.Single();

            var ex = Assert.Throws<DomainException>(() => supplier.DeactivateBankAccount(account, LATER));

            Assert.Equal("AP.SUP03", ex.Id);
        }

        // Desativar uma conta quando há outras restantes move o VO para InactiveBankAccounts e emite Deactivated com snapshot.
        [Fact]
        public void DeactivateBankAccount_WhenMoreThanOneAccount_ShouldMoveToInactiveAndEmitEvent()
        {
            var supplier = SupplierMother.ActiveWithBankAccounts(count: 2);
            supplier.PullDomainEvents();
            var firstAccount = supplier.ActiveBankAccounts.First();

            supplier.DeactivateBankAccount(firstAccount, LATER);

            Assert.Single(supplier.ActiveBankAccounts);
            Assert.Single(supplier.InactiveBankAccounts);
            Assert.Equal(firstAccount, supplier.InactiveBankAccounts.Single());
            var deactivated = Assert.IsType<SupplierBankAccountDeactivated>(supplier.PullDomainEvents()[0]);
            Assert.Equal("BANK_TRANSFER", deactivated.Variant);
            var firstAsBank = Assert.IsType<SupplierBankTransferAccount>(firstAccount);
            Assert.Equal(firstAsBank.AccountNumber, deactivated.AccountNumber);
        }

        // Desativar a última conta quando o Supplier está Inactive é permitido (invariante só vale para Active).
        [Fact]
        public void DeactivateBankAccount_WhenLastAccountAndInactive_ShouldDeactivateSuccessfully()
        {
            var supplier = SupplierMother.Active();
            var account = SupplierMother.BankTransferAccount();
            supplier.AddBankAccount(account, LATER);
            supplier.Deactivate(LATER.AddMinutes(1));
            supplier.PullDomainEvents();

            supplier.DeactivateBankAccount(account, LATER.AddMinutes(2));

            Assert.Empty(supplier.ActiveBankAccounts);
            Assert.Single(supplier.InactiveBankAccounts);
        }

        // Re-adicionar uma conta com os mesmos dados após desativar é permitido — a nova entrada vai para ativos
        // e a entrada antiga permanece em inativos (preservação do histórico).
        [Fact]
        public void AddBankAccount_AfterDeactivation_ShouldReaddAndKeepInactiveSnapshot()
        {
            var supplier = SupplierMother.ActiveWithBankAccounts(count: 2);
            var first = supplier.ActiveBankAccounts.First();
            supplier.DeactivateBankAccount(first, LATER);
            supplier.PullDomainEvents();

            supplier.AddBankAccount(first, LATER.AddMinutes(1));

            Assert.Contains(first, supplier.ActiveBankAccounts);
            Assert.Contains(first, supplier.InactiveBankAccounts);
        }
    }

    public class WhenBlocking
    {
        // Block muda status para Blocked, emite SupplierBlocked com a razão informada.
        [Fact]
        public void Block_FromActive_ShouldChangeStatusAndEmitEvent()
        {
            var supplier = SupplierMother.Active();
            supplier.PullDomainEvents();

            supplier.Block("Inadimplência", LATER);

            Assert.Equal(SupplierStatus.Blocked, supplier.Status);
            var blocked = Assert.IsType<SupplierBlocked>(supplier.PullDomainEvents()[0]);
            Assert.Equal("Inadimplência", blocked.Reason);
        }

        // Block exige motivo não vazio — string vazia/branca lança AP.SUP02.
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Block_WithEmptyReason_ShouldThrowDomainException(string reason)
        {
            var supplier = SupplierMother.Active();

            var ex = Assert.Throws<DomainException>(() => supplier.Block(reason, LATER));

            Assert.Equal("AP.SUP02", ex.Id);
        }

        // Block sobre um Supplier já Blocked lança AP.SUP01 (transição inválida).
        [Fact]
        public void Block_WhenAlreadyBlocked_ShouldThrowDomainException()
        {
            var supplier = SupplierMother.Blocked();

            var ex = Assert.Throws<DomainException>(() => supplier.Block("Outra razão", LATER));

            Assert.Equal("AP.SUP01", ex.Id);
        }

        // Block sobre Supplier Inactive não é permitido (transição Inactive->Blocked é inválida).
        [Fact]
        public void Block_WhenInactive_ShouldThrowDomainException()
        {
            var supplier = SupplierMother.Inactive();

            var ex = Assert.Throws<DomainException>(() => supplier.Block("Razão", LATER));

            Assert.Equal("AP.SUP01", ex.Id);
        }
    }

    public class WhenUnblocking
    {
        // Unblock de Supplier Blocked retorna o status para Active e emite SupplierUnblocked.
        [Fact]
        public void Unblock_FromBlocked_ShouldChangeStatusAndEmitEvent()
        {
            var supplier = SupplierMother.Blocked();
            supplier.PullDomainEvents();

            supplier.Unblock(LATER);

            Assert.Equal(SupplierStatus.Active, supplier.Status);
            Assert.IsType<SupplierUnblocked>(supplier.PullDomainEvents()[0]);
        }

        // Unblock em status que não é Blocked lança AP.SUP01.
        [Fact]
        public void Unblock_WhenActive_ShouldThrowDomainException()
        {
            var supplier = SupplierMother.Active();

            var ex = Assert.Throws<DomainException>(() => supplier.Unblock(LATER));

            Assert.Equal("AP.SUP01", ex.Id);
        }
    }

    public class WhenDeactivating
    {
        // Deactivate de Active vai para Inactive e emite SupplierDeactivated.
        [Fact]
        public void Deactivate_FromActive_ShouldChangeStatusAndEmitEvent()
        {
            var supplier = SupplierMother.Active();
            supplier.PullDomainEvents();

            supplier.Deactivate(LATER);

            Assert.Equal(SupplierStatus.Inactive, supplier.Status);
            Assert.IsType<SupplierDeactivated>(supplier.PullDomainEvents()[0]);
        }

        // Deactivate em status não-Active lança AP.SUP01 (transição inválida de Blocked/Inactive).
        [Fact]
        public void Deactivate_WhenBlocked_ShouldThrowDomainException()
        {
            var supplier = SupplierMother.Blocked();

            var ex = Assert.Throws<DomainException>(() => supplier.Deactivate(LATER));

            Assert.Equal("AP.SUP01", ex.Id);
        }
    }

    public class WhenReactivating
    {
        // Reactivate de Inactive retorna para Active e emite SupplierReactivated.
        [Fact]
        public void Reactivate_FromInactive_ShouldChangeStatusAndEmitEvent()
        {
            var supplier = SupplierMother.Inactive();
            supplier.PullDomainEvents();

            supplier.Reactivate(LATER);

            Assert.Equal(SupplierStatus.Active, supplier.Status);
            Assert.IsType<SupplierReactivated>(supplier.PullDomainEvents()[0]);
        }

        // Reactivate de status diferente de Inactive lança AP.SUP01.
        [Fact]
        public void Reactivate_WhenActive_ShouldThrowDomainException()
        {
            var supplier = SupplierMother.Active();

            var ex = Assert.Throws<DomainException>(() => supplier.Reactivate(LATER));

            Assert.Equal("AP.SUP01", ex.Id);
        }
    }
}
