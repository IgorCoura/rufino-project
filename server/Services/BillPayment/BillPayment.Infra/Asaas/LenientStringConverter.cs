namespace BillPayment.Infra.Asaas;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Lê número, texto ou booleano do provedor como texto.
/// </summary>
/// <remarks>
/// Os campos de valor e data dos DTOs são <c>string?</c> de propósito: o provedor devolve
/// <c>""</c> para data ausente em parte das respostas de arrecadação, e desserializar direto
/// para <c>DateOnly?</c> faria a consulta inteira falhar por um campo que o modelo já trata
/// como opcional. Mas ele devolve <em>valor</em> como número JSON — sem este conversor, a
/// mesma resposta falha pelo motivo oposto. As duas frouxidões precisam existir juntas.
/// </remarks>
internal sealed class LenientStringConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.GetDecimal().ToString(CultureInfo.InvariantCulture),
            JsonTokenType.True => bool.TrueString,
            JsonTokenType.False => bool.FalseString,
            _ => throw new JsonException($"Token inesperado ao ler texto: {reader.TokenType}."),
        };

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value);
    }
}
