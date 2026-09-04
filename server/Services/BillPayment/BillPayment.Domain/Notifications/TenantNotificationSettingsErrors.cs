namespace BillPayment.Domain.Notifications;

using System.IO;
using System.Runtime.CompilerServices;
using BillPayment.Domain.SeedWork;

// BC: BLP (BillPayment) — Aggregate: NTF (TenantNotificationSettings)
// Public porque a Application lança NotFound e as pré-condições de cadastro.
public static class TenantNotificationSettingsErrors
{
    private const string AGGREGATE_PREFIX = "BLP.NTF";

    public static DomainException NotFound(
        Guid tenantId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}00",
            messageTemplate: "Nenhuma configuração de aviso encontrada para o tenant {0}.",
            parameters: new object[] { tenantId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.NotFound);

    /// <summary>
    /// Endereço ilegível recusado no cadastro. O aviso é a rede de segurança do ADR-014 — deixar
    /// entrar endereço inválido produziria configuração que parece feita e nunca entrega nada.
    /// </summary>
    public static DomainException InvalidRecipient(
        string address,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}01",
            messageTemplate: "'{0}' não é um endereço de e-mail válido.",
            parameters: new object[] { address },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// Teto de destinatários. Aviso não é lista de distribuição, e o custo de envio é por
    /// endereço a cada nível de escalonamento de cada ciclo.
    /// </summary>
    public static DomainException TooManyRecipients(
        int maximum,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}02",
            messageTemplate: "A configuração de avisos aceita no máximo {0} destinatários.",
            parameters: new object[] { maximum },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// Ligar o aviso sem ter para quem mandar descreveria um canal que não existe — e o modo de
    /// falha desse engano é justamente o silêncio que o ADR-014 combate.
    /// </summary>
    public static DomainException EnabledWithoutRecipients(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}03",
            messageTemplate: "Não é possível ligar os avisos sem nenhum destinatário cadastrado.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
