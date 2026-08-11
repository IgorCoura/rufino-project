namespace BillPayment.Infra.DocumentIntelligence.Gemini;

using System.Globalization;
using System.Text;
using System.Text.Json;
using BillPayment.Domain.Extraction;

/// <summary>
/// O prompt e o schema de resposta — detalhe de implementação deste provedor.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Não é configuração.</strong> Promovê-lo a <c>appsettings</c> convidaria a ajustá-lo
/// sem rodar a métrica do corpus, e faria o BC ter opinião sobre como se fala com um modelo —
/// exatamente o acoplamento que o ADR-013 evita. Provedores diferentes pedem prompts diferentes;
/// quando a IA trocar, este arquivo morre junto com o adapter.
/// </para>
/// <para>
/// <strong>O prompt pede transcrição, não interpretação.</strong> Ele manda copiar o que está
/// impresso e devolver lista vazia quando não houver nada — porque o custo de um número
/// inventado é alto e o de um campo vazio é zero: a cascata já sabe lidar com "não achei".
/// </para>
/// </remarks>
internal static class GeminiPrompt
{
    /// <summary>
    /// A forma que o modelo é obrigado a devolver.
    /// </summary>
    /// <remarks>
    /// Valor e vencimento são <c>string</c> de propósito: o modelo devolve o que está impresso
    /// (<c>"R$ 1.234,56"</c>, <c>"12/08/2026"</c>), e forçá-lo a converter para número ou data
    /// no schema faz ele <em>adivinhar</em> quando o campo está borrado. A conversão frouxa
    /// acontece no adapter, onde falhar é barato.
    /// </remarks>
    public static JsonElement ResponseSchema { get; } = JsonSerializer.Deserialize<JsonElement>(
        """
        {
          "type": "object",
          "properties": {
            "digitableLines": { "type": "array", "items": { "type": "string" } },
            "pixPayloads":    { "type": "array", "items": { "type": "string" } },
            "documentKind":   { "type": "string", "enum": ["BankSlip", "Utility", "TaxGuide", "NotABill"] },
            "payerName":        { "type": "string" },
            "payerTaxId":       { "type": "string" },
            "payeeName":        { "type": "string" },
            "payeeTaxId":       { "type": "string" },
            "accountReference": { "type": "string" },
            "amount":           { "type": "string" },
            "dueDate":          { "type": "string" },
            "notes":            { "type": "string" }
          },
          "required": ["digitableLines", "pixPayloads", "documentKind"]
        }
        """);

    public static string Build(ExtractionHints hints)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine("Você transcreve documentos de cobrança brasileiros. NÃO interprete: copie o que está impresso.");
        prompt.AppendLine();
        prompt.AppendLine("Devolva:");
        prompt.AppendLine("- digitableLines: TODA linha digitável visível, com os dígitos exatamente como impressos.");
        prompt.AppendLine("  Boleto de cobrança tem 47 dígitos; guia de arrecadação (FGTS, DARF, GPS, sindicato,");
        prompt.AppendLine("  concessionária) tem 48 e começa com 8. Se só houver o código de barras, transcreva os 44 dígitos.");
        prompt.AppendLine("- pixPayloads: o BR Code completo (\"copia e cola\"), começando por 000201. Só se estiver");
        prompt.AppendLine("  escrito como texto; NÃO tente decodificar a imagem do QR.");
        prompt.AppendLine("- documentKind: BankSlip, Utility, TaxGuide ou NotABill.");
        prompt.AppendLine("- accountReference: instalação, matrícula, unidade ou contrato, quando houver.");
        prompt.AppendLine();
        prompt.AppendLine("REGRAS:");
        prompt.AppendLine("1. Nunca invente dígito. Ilegível ou parcial: omita o candidato inteiro.");
        prompt.AppendLine("2. Na dúvida entre dois dígitos, devolva as duas leituras como candidatos separados.");
        prompt.AppendLine("3. Documento sem nada a pagar: listas vazias e documentKind NotABill. É resposta correta.");
        prompt.AppendLine("4. Não some, não converta e não corrija valores — transcreva.");

        AppendHints(prompt, hints);

        return prompt.ToString();
    }

    /// <summary>
    /// O que o sistema já sabe, para reduzir alucinação em campo cortado ou borrado.
    /// </summary>
    /// <remarks>
    /// Um modelo que já viu "estes são os documentos do pagador" tem menos margem para inventar
    /// um. Continua sendo dica, não autoridade — o DV é quem decide. Isto sai do perímetro junto
    /// com o documento, por escolha consciente registrada no doc 10.
    /// </remarks>
    private static void AppendHints(StringBuilder prompt, ExtractionHints hints)
    {
        if (hints.PayerTaxIds.Count == 0 && hints.KnownPayeeNames.Count == 0)
            return;

        prompt.AppendLine();
        prompt.AppendLine("CONTEXTO (para conferir leitura, nunca para preencher o que não está no documento):");

        if (hints.PayerTaxIds.Count > 0)
        {
            prompt.AppendLine(CultureInfo.InvariantCulture, $"- Documentos do pagador: {string.Join(", ", hints.PayerTaxIds)}");
        }

        if (hints.KnownPayeeNames.Count > 0)
        {
            prompt.AppendLine(CultureInfo.InvariantCulture, $"- Beneficiários conhecidos: {string.Join(", ", hints.KnownPayeeNames)}");
        }
    }
}
