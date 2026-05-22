namespace AccountsPayable.Domain.Payables.Enumerations;

using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Channel through which a <see cref="Payable"/> will be paid. Decidido na criação do Payable
/// (Sprint 12.B); carregado no <c>PayablePaymentRequested</c> para o sibling
/// <c>PaymentExecution</c> escolher o adapter Asaas adequado.
/// <list type="bullet">
/// <item><see cref="SupplierTransfer"/> — transferência genérica para a conta cadastrada do fornecedor;
/// o PSP escolhe entre PIX-via-DICT e TED conforme disponibilidade no <c>SupplierBankAccount</c>.</item>
/// <item><see cref="DynamicPix"/> — PIX dinâmico via EMV BR Code (cobre boleto-PIX, copia-e-cola e
/// chave temporária — todos têm a mesma estrutura EMV).</item>
/// <item><see cref="BankSlip"/> — boleto convencional via código de barras (linha digitável opcional).</item>
/// </list>
/// </summary>
public sealed class PaymentMethod : Enumeration
{
    public static readonly PaymentMethod SupplierTransfer = new(1, "SUPPLIER_TRANSFER");
    public static readonly PaymentMethod DynamicPix = new(2, "DYNAMIC_PIX");
    public static readonly PaymentMethod BankSlip = new(3, "BANK_SLIP");

    private PaymentMethod(int id, string name) : base(id, name) { }
}
