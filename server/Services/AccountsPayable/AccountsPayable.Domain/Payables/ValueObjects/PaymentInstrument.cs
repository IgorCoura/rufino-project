namespace AccountsPayable.Domain.Payables.ValueObjects;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers.Enumerations;
using AccountsPayable.Domain.Suppliers.ValueObjects;

/// <summary>
/// Instrumento de pagamento associado a um <see cref="Payable"/>. Define <b>como</b> o pagamento
/// será executado, incluindo o "código" necessário para o PSP processar (EMV, código de barras,
/// ou snapshot dos dados do fornecedor). Selado polimórfico: <see cref="SupplierPixTransferInstrument"/>,
/// <see cref="SupplierBankTransferInstrument"/>, <see cref="DynamicPixInstrument"/>, <see cref="BankSlipInstrument"/>.
/// <para>
/// Decidido na criação do <c>Payable</c> (Sprint 12.B); imutável. Trocar instrumento = substituição
/// (padrão D-134) que cancela o Payable e cria um novo.
/// </para>
/// </summary>
public abstract class PaymentInstrument : ValueObject
{
    private protected PaymentInstrument() { }

    /// <summary>Método derivado da variante concreta — usado pelo PSP para escolher o adapter.</summary>
    public abstract PaymentMethod Method { get; }
}

/// <summary>
/// Variantes de transferência para conta do fornecedor — o PSP decide entre PIX-via-DICT e TED.
/// Carrega snapshot de <see cref="SupplierLegalName"/> e <see cref="SupplierTaxId"/> congelados
/// no momento da criação do Payable, para suportar emissão de TED sem precisar consultar o
/// estado atual do <see cref="Supplier"/>.
/// </summary>
public abstract class SupplierTransferInstrument : PaymentInstrument
{
    public LegalName SupplierLegalName { get; }
    public TaxId SupplierTaxId { get; }

    public override PaymentMethod Method => PaymentMethod.SupplierTransfer;

    private protected SupplierTransferInstrument(LegalName supplierLegalName, TaxId supplierTaxId)
    {
        ArgumentNullException.ThrowIfNull(supplierLegalName);
        ArgumentNullException.ThrowIfNull(supplierTaxId);
        SupplierLegalName = supplierLegalName;
        SupplierTaxId = supplierTaxId;
    }
}

/// <summary>
/// Transferência para chave PIX estática do fornecedor (snapshot da <see cref="PixKey"/>).
/// </summary>
public sealed class SupplierPixTransferInstrument : SupplierTransferInstrument
{
    public PixKey PixKey { get; }

    public SupplierPixTransferInstrument(LegalName supplierLegalName, TaxId supplierTaxId, PixKey pixKey)
        : base(supplierLegalName, supplierTaxId)
    {
        ArgumentNullException.ThrowIfNull(pixKey);
        PixKey = pixKey;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SupplierLegalName;
        yield return SupplierTaxId;
        yield return PixKey;
    }
}

/// <summary>
/// Transferência para conta bancária tradicional do fornecedor (snapshot de banco/agência/número/tipo).
/// Reutiliza os normalizadores e erros de <see cref="SupplierBankAccountErrors"/> (sigla SBA).
/// </summary>
public sealed class SupplierBankTransferInstrument : SupplierTransferInstrument
{
    public const int BANK_CODE_LENGTH = 3;

    public string BankCode { get; }
    public string Branch { get; }
    public string AccountNumber { get; }
    public BankAccountType AccountType { get; }

    public SupplierBankTransferInstrument(
        LegalName supplierLegalName,
        TaxId supplierTaxId,
        string bankCode,
        string branch,
        string accountNumber,
        BankAccountType accountType)
        : base(supplierLegalName, supplierTaxId)
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
        yield return SupplierLegalName;
        yield return SupplierTaxId;
        yield return BankCode;
        yield return Branch;
        yield return AccountNumber;
        yield return AccountType;
    }
}

/// <summary>
/// PIX dinâmico via EMV BR Code — unifica boleto-PIX, copia-e-cola e chave temporária.
/// </summary>
public sealed class DynamicPixInstrument : PaymentInstrument
{
    public EmvPayload Payload { get; }

    public override PaymentMethod Method => PaymentMethod.DynamicPix;

    public DynamicPixInstrument(EmvPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        Payload = payload;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Payload;
    }
}

/// <summary>
/// Boleto bancário convencional — carrega apenas o <see cref="BarcodeDigits"/> (44 dígitos) como
/// forma canônica. A <see cref="ValueObjects.DigitableLine"/> (47 dígitos) é representação
/// alternativa derivável do código de barras via <see cref="BarcodeDigits.ToDigitableLine"/>;
/// não vive no instrumento para evitar redundância de estado e inconsistência potencial.
/// </summary>
public sealed class BankSlipInstrument : PaymentInstrument
{
    public BarcodeDigits Barcode { get; }

    public override PaymentMethod Method => PaymentMethod.BankSlip;

    public BankSlipInstrument(BarcodeDigits barcode)
    {
        ArgumentNullException.ThrowIfNull(barcode);
        Barcode = barcode;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Barcode;
    }
}
