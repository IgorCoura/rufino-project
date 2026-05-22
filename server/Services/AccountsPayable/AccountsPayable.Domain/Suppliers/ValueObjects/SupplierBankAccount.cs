namespace AccountsPayable.Domain.Suppliers.ValueObjects;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers.Enumerations;

/// <summary>
/// Forma como o fornecedor recebe pagamento. Value Object imutável; hierarquia selada — uma conta
/// ou é PIX (chave estática) ou é dados bancários tradicionais (banco/agência/número/tipo), nunca
/// ambos. Igualdade estrutural por componentes da variante; instâncias de variantes diferentes
/// nunca são iguais porque <see cref="ValueObject.Equals(object?)"/> compara <c>GetType()</c>
/// antes dos componentes.
/// </summary>
public abstract class SupplierBankAccount : ValueObject
{
    private protected SupplierBankAccount() { }
}

/// <summary>
/// Conta PIX — chave estática cadastrada (CPF/CNPJ/email/telefone/aleatória). Quando usada em um
/// pagamento, o PSP executa via PIX direto na chave; não há agência/número de conta a expor.
/// </summary>
public sealed class SupplierPixAccount : SupplierBankAccount
{
    public PixKey PixKey { get; }

    public SupplierPixAccount(PixKey pixKey)
    {
        ArgumentNullException.ThrowIfNull(pixKey);
        PixKey = pixKey;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PixKey;
    }
}

/// <summary>
/// Conta bancária tradicional. Quando usada em um pagamento, o PSP decide entre TED/DOC/PIX-via-DICT
/// conforme disponibilidade — o Domain apenas declara os dados da conta destino, sem amarrar canal.
/// </summary>
public sealed class SupplierBankTransferAccount : SupplierBankAccount
{
    public const int BANK_CODE_LENGTH = 3;

    public string BankCode { get; }
    public string Branch { get; }
    public string AccountNumber { get; }
    public BankAccountType AccountType { get; }

    public SupplierBankTransferAccount(
        string bankCode,
        string branch,
        string accountNumber,
        BankAccountType accountType)
    {
        if (accountType is null)
            throw SupplierBankAccountErrors.AccountTypeRequired();

        BankCode = NormalizeBankCode(bankCode);
        Branch = NormalizeBranch(branch);
        AccountNumber = NormalizeAccountNumber(accountNumber);
        AccountType = accountType;
    }

    private static string NormalizeBankCode(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw SupplierBankAccountErrors.BankCodeEmpty();
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length != BANK_CODE_LENGTH)
            throw SupplierBankAccountErrors.BankCodeInvalid(raw);
        return digits;
    }

    private static string NormalizeBranch(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw SupplierBankAccountErrors.BranchEmpty();
        return raw.Trim();
    }

    private static string NormalizeAccountNumber(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw SupplierBankAccountErrors.AccountNumberEmpty();
        return raw.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return BankCode;
        yield return Branch;
        yield return AccountNumber;
        yield return AccountType;
    }
}
