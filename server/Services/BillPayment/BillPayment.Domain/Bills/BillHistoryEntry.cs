namespace BillPayment.Domain.Bills;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Uma linha da trilha do boleto: o que foi feito, quando, e por quem.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É Value Object, pelo mesmo motivo do <c>BillCheck</c>:</strong> a entrada nasce e
/// nunca mais muda, ninguém a referencia por id, e trocá-la por outra de atributos idênticos não
/// quebraria nada. Identidade seria ficção. Persistida em <c>bill_history_entries</c> como
/// coleção owned.
/// </para>
/// <para>
/// <strong><see cref="ActorName"/> é desnormalizado de propósito.</strong> Trilha de auditoria
/// grava o nome <em>como ele era no instante da ação</em> — é a diferença entre "quem fez" e
/// "como essa pessoa se chama hoje". Além disso este BC não tem cadastro de pessoas: guardar só
/// o <see cref="ActorUserId"/> faria a tela mostrar um GUID, ou criaria dependência de outro
/// contexto para renderizar histórico. Vem do token na borda, mesma doutrina do <c>sub</c>.
/// </para>
/// <para>
/// <see cref="ActorUserId"/> nulo significa <strong>sistema</strong> — outbox, webhook,
/// conciliação. Não é ausência de informação: é a informação de que ninguém decidiu aquilo.
/// </para>
/// </remarks>
public sealed class BillHistoryEntry : ValueObject
{
    public const int ACTOR_NAME_MAX_LENGTH = 200;
    public const int NOTE_MAX_LENGTH = 500;

    /// <summary>O nome gravado quando a ação partiu da nossa própria automação.</summary>
    public const string SYSTEM_ACTOR_NAME = "Sistema";

    /// <summary>
    /// O nome gravado quando a ação partiu do provedor de pagamento.
    /// </summary>
    /// <remarks>
    /// Diz "no provedor" e não "Asaas" porque o nome do fornecedor não entra em dado persistido
    /// (convenção do realm, e a mesma razão de o client se chamar <c>document-signing-client</c>):
    /// trocar de provedor não pode obrigar a reescrever histórico.
    /// </remarks>
    public const string PROVIDER_ACTOR_NAME = "Provedor de pagamento";

    public BillAction Action { get; private set; } = default!;

    /// <summary>
    /// De onde partiu — nosso app, o provedor, ou automação nossa. É o que impede a trilha de
    /// dizer "Sistema" para um cancelamento feito no painel do provedor.
    /// </summary>
    public BillActionOrigin Origin { get; private set; } = default!;

    public DateTime OccurredAt { get; private set; }

    /// <summary>
    /// Quem fez. Nulo quando a origem não carrega autor identificável — provedor e automação.
    /// </summary>
    public UserId? ActorUserId { get; private set; }

    /// <summary>
    /// O nome de quem fez, congelado no instante da ação; ou de ONDE veio, quando não há pessoa.
    /// </summary>
    public string ActorName { get; private set; } = SYSTEM_ACTOR_NAME;

    /// <summary>De onde saiu. Nulo na primeira entrada, que não tem "antes".</summary>
    public BillStatus? FromStatus { get; private set; }

    /// <summary>Para onde foi.</summary>
    public BillStatus ToStatus { get; private set; } = default!;

    /// <summary>
    /// O motivo, quando a ação exige um, ou o detalhe que a torna compreensível meses depois —
    /// a data pedida no agendamento, por exemplo.
    /// </summary>
    public string? Note { get; private set; }

    private BillHistoryEntry() { }

    internal static BillHistoryEntry Record(
        BillAction action,
        BillActionOrigin origin,
        BillStatus? fromStatus,
        BillStatus toStatus,
        UserId? actorUserId,
        string? actorName,
        DateTime occurredAt,
        string? note = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(toStatus);

        // UserId.Empty é "não veio ninguém no token" — não vira pessoa vazia. E autor só é
        // aceito quando a ORIGEM o admite: um id chegando junto de uma mudança do provedor seria
        // atribuir a alguém um ato que essa pessoa não praticou.
        var actor = origin.CarriesActor && actorUserId is { } id && id != UserId.Empty
            ? id
            : (UserId?)null;

        return new BillHistoryEntry
        {
            Action = action,
            Origin = origin,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ActorUserId = actor,
            ActorName = ResolveActorName(origin, actor, actorName),
            OccurredAt = occurredAt,
            Note = Trim(note, NOTE_MAX_LENGTH),
        };
    }

    private static string ResolveActorName(BillActionOrigin origin, UserId? actor, string? actorName)
    {
        if (actor is null)
            return origin == BillActionOrigin.Provider ? PROVIDER_ACTOR_NAME : SYSTEM_ACTOR_NAME;

        // Pessoa sem nome no token não vira "Sistema": mentiria sobre a natureza da ação. Fica o
        // id, que ao menos é rastreável até o Keycloak.
        return Trim(actorName, ACTOR_NAME_MAX_LENGTH) ?? actor.Value.Value.ToString();
    }

    private static string? Trim(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Action;
        yield return Origin;
        yield return OccurredAt;
        yield return ActorUserId;
        yield return ActorName;
        yield return FromStatus;
        yield return ToStatus;
        yield return Note;
    }
}
