namespace BillPayment.Infra.Mapping;

using System.Text.Json;
using BillPayment.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// <see cref="TaxId"/> vira <strong>uma coluna de texto</strong>, não owned type. O tipo do
/// documento não precisa de coluna própria porque é dedutível do valor — 11 dígitos é CPF,
/// 14 é CNPJ — e <see cref="TaxId.Parse"/> é a única implementação dessa dedução.
/// </summary>
/// <remarks>
/// A escolha também é o que torna possível o índice único <c>(tenant_id, tax_id)</c>: o
/// construtor de índice do EF só aceita propriedade do próprio tipo, e uma propriedade de
/// owned type não é endereçável a partir da raiz.
/// <para>
/// A rehidratação passa por <c>Parse</c>, então dígito verificador corrompido no banco falha
/// alto na leitura em vez de virar documento inválido circulando pelo domínio.
/// </para>
/// </remarks>
internal static class TaxIdConversions
{
    /// <summary>Comprimento do CNPJ, que é o maior dos dois formatos.</summary>
    public const int MAX_LENGTH = 14;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static readonly ValueConverter<TaxId, string> Single =
        new(taxId => taxId.Value, value => TaxId.Parse(value));

    public static readonly ValueComparer<TaxId> SingleComparer =
        new((left, right) => left == null ? right == null : left.Equals(right),
            taxId => taxId.GetHashCode(),
            taxId => taxId);

    public static readonly ValueConverter<IReadOnlyCollection<TaxId>, string> Collection =
        new(taxIds => JsonSerializer.Serialize(taxIds.Select(t => t.Value).ToList(), Json),
            json => JsonSerializer.Deserialize<List<string>>(json, Json)!.ConvertAll(TaxId.Parse));

    public static readonly ValueComparer<IReadOnlyCollection<TaxId>> CollectionComparer =
        new((left, right) => left!.SequenceEqual(right!),
            taxIds => taxIds.Aggregate(0, (hash, t) => HashCode.Combine(hash, t.GetHashCode())),
            taxIds => taxIds.ToList());
}
