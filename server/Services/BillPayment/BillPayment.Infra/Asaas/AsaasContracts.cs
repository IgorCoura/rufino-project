namespace BillPayment.Infra.Asaas;

using System.Text.Json;
using System.Text.Json.Serialization;

// DTOs do provedor. Nenhum deles cruza a fronteira da Infra — os adapters traduzem para os
// VOs de BillPayment.Domain.Lookups antes de devolver.
//
// Datas e valores chegam como string de propósito: o provedor devolve "" para data ausente em
// parte das respostas de arrecadação, e desserializar direto para DateOnly? faria a consulta
// inteira falhar por causa de um campo que o modelo já trata como opcional. Como valor vem
// como número JSON, esses mesmos campos usam LenientStringConverter — as duas frouxidões
// precisam existir juntas, senão a mesma resposta falha por um dos dois lados.

internal sealed class AsaasErrorResponse
{
    [JsonPropertyName("errors")]
    public List<AsaasError>? Errors { get; set; }
}

internal sealed class AsaasError
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

internal sealed class AsaasSimulateResponse
{
    [JsonPropertyName("bankSlipInfo")]
    public AsaasBankSlipInfo? BankSlipInfo { get; set; }

    [JsonPropertyName("fee")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Fee { get; set; }

    [JsonPropertyName("minimumScheduleDate")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? MinimumScheduleDate { get; set; }
}

internal sealed class AsaasBankSlipInfo
{
    [JsonPropertyName("beneficiaryName")]
    public string? BeneficiaryName { get; set; }

    [JsonPropertyName("beneficiaryCpfCnpj")]
    public string? BeneficiaryCpfCnpj { get; set; }

    /// <summary>Nome comercial. Em arrecadação é o único identificador que volta (100% do corpus).</summary>
    [JsonPropertyName("companyName")]
    public string? CompanyName { get; set; }

    /// <summary>
    /// Nunca foi observado preenchido (0% em arrecadação, e cobrança não resolve em sandbox),
    /// então a forma é desconhecida. Lido como <see cref="JsonElement"/> para aceitar tanto a
    /// string do código quanto um objeto com <c>code</c> — adivinhar errado aqui derrubaria a
    /// resposta inteira num campo que é só conferência cruzada.
    /// </summary>
    [JsonPropertyName("bank")]
    public JsonElement? Bank { get; set; }

    [JsonPropertyName("value")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Value { get; set; }

    [JsonPropertyName("originalValue")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? OriginalValue { get; set; }

    [JsonPropertyName("minValue")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? MinValue { get; set; }

    [JsonPropertyName("maxValue")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? MaxValue { get; set; }

    [JsonPropertyName("allowChangeValue")]
    public bool? AllowChangeValue { get; set; }

    [JsonPropertyName("dueDate")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? DueDate { get; set; }

    [JsonPropertyName("isOverdue")]
    public bool? IsOverdue { get; set; }

    [JsonPropertyName("interestValue")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? InterestValue { get; set; }

    [JsonPropertyName("fineValue")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? FineValue { get; set; }

    [JsonPropertyName("discountValue")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? DiscountValue { get; set; }
}

internal sealed class AsaasPixDecodeRequest
{
    [JsonPropertyName("payload")]
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// A instituição recalcula juros, multa e desconto para esta data. Omitido, o provedor
    /// assume hoje — e o valor devolvido não seria o que será debitado no agendamento.
    /// </summary>
    [JsonPropertyName("expectedPaymentDate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExpectedPaymentDate { get; set; }
}

internal sealed class AsaasPixDecodeResponse
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("receiver")]
    public AsaasPixParty? Receiver { get; set; }

    [JsonPropertyName("payer")]
    public AsaasPixParty? Payer { get; set; }

    [JsonPropertyName("value")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Value { get; set; }

    [JsonPropertyName("totalValue")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? TotalValue { get; set; }

    [JsonPropertyName("interest")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Interest { get; set; }

    [JsonPropertyName("fine")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Fine { get; set; }

    [JsonPropertyName("discount")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Discount { get; set; }

    [JsonPropertyName("changeValue")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? ChangeValue { get; set; }

    [JsonPropertyName("dueDate")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? DueDate { get; set; }

    [JsonPropertyName("expirationDate")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? ExpirationDate { get; set; }

    [JsonPropertyName("canBePaid")]
    public bool? CanBePaid { get; set; }

    [JsonPropertyName("cannotBePaidReason")]
    public string? CannotBePaidReason { get; set; }

    [JsonPropertyName("canBePaidWithDifferentValue")]
    public bool? CanBePaidWithDifferentValue { get; set; }

    [JsonPropertyName("conciliationIdentifier")]
    public string? ConciliationIdentifier { get; set; }

    /// <summary>
    /// Descrição da cobrança. Medido em produção (2026-08-06) — não estava na leitura inicial
    /// da documentação. É o campo que diz ao aprovador <em>do que se trata</em> a cobrança.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

internal sealed class AsaasPixParty
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("tradingName")]
    public string? TradingName { get; set; }

    /// <summary>Do recebedor vem completo; do pagador vem <strong>mascarado</strong>.</summary>
    [JsonPropertyName("cpfCnpj")]
    public string? CpfCnpj { get; set; }

    [JsonPropertyName("ispb")]
    public string? Ispb { get; set; }

    [JsonPropertyName("ispbName")]
    public string? IspbName { get; set; }

    [JsonPropertyName("personType")]
    public string? PersonType { get; set; }

    [JsonPropertyName("accountType")]
    public string? AccountType { get; set; }
}
