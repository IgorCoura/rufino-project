namespace BillPayment.Application.Bills.Commands;

using BillPayment.Domain.Bills;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Política de aprovação vinda de configuração.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É política de negócio, não de infraestrutura</strong> — por isso mora na Application,
/// junto do caso de uso que a consome, e não em <c>Infra/</c> com as opções de banco e provedor.
/// </para>
/// <para>
/// <see cref="DefaultApprovalLimit"/> é um teto <strong>único por instalação</strong>. A alçada
/// por pessoa que o roadmap pede depende da identidade vinda do Keycloak (fase 6); até lá este
/// valor é o que existe, e o formato do <c>ApprovalPolicy</c> já é o final — o dia da amarração
/// muda de onde o número vem, não o domínio.
/// </para>
/// </remarks>
public sealed class ApprovalOptions
{
    public const string SectionName = "Approval";

    /// <summary>Prazo de validade do retrato da consulta na hora de aprovar.</summary>
    public int MaxSnapshotAgeHours { get; set; } = ApprovalPolicy.DEFAULT_MAX_SNAPSHOT_AGE_HOURS;

    /// <summary>Teto de valor. Nulo significa sem teto — não significa zero.</summary>
    public decimal? DefaultApprovalLimit { get; set; }

    public ApprovalPolicy ToPolicy()
        => ApprovalPolicy.Of(
            TimeSpan.FromHours(MaxSnapshotAgeHours <= 0 ? ApprovalPolicy.DEFAULT_MAX_SNAPSHOT_AGE_HOURS : MaxSnapshotAgeHours),
            DefaultApprovalLimit is null ? null : new Money(DefaultApprovalLimit.Value, Currency.BRL));
}
