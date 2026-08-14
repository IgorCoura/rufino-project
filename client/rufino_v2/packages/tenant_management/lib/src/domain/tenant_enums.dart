/// Wire values of the backend's `TenantKind` smart enum.
///
/// The strings are the contract — they travel in the payload and come back in
/// the read model. The labels beside them are presentation, and only exist so
/// a screen never has to invent its own translation.
abstract final class TenantKinds {
  /// A natural person. Identified by CPF, never has a trade name.
  static const String individual = 'Individual';

  /// A legal entity. Identified by CNPJ, may have a trade name.
  static const String company = 'Company';

  /// The label to show for [kind].
  static String label(String kind) =>
      kind == individual ? 'Pessoa física' : 'Pessoa jurídica';
}

/// Wire values of the backend's `TenantStatus` smart enum.
abstract final class TenantStatuses {
  /// The cadastro is live.
  static const String active = 'Active';

  /// The cadastro is preserved but frozen: no change is accepted.
  static const String suspended = 'Suspended';

  /// The label to show for [status].
  static String label(String status) =>
      status == suspended ? 'Suspenso' : 'Ativo';
}

/// Wire values of the backend's `MembershipRole` smart enum.
abstract final class MembershipRoles {
  /// Answers for the tenant. There is always at least one.
  static const String owner = 'Owner';

  /// Takes part in the tenant without answering for it.
  static const String member = 'Member';

  /// The label to show for [role].
  static String label(String role) =>
      role == owner ? 'Responsável' : 'Membro';
}

/// Wire values of the backend's `ProvisioningStatus` smart enum.
///
/// This is the state of the access grant **at the identity provider**, which
/// does not take part in the database transaction: a tenant can be registered
/// and still unreachable, and this is the field that says so.
abstract final class ProvisioningStatuses {
  /// Sent, not confirmed yet.
  static const String pending = 'Pending';

  /// The identity provider has the grant.
  static const String done = 'Done';

  /// The identity provider refused or was unreachable. Curable by
  /// reprovisioning.
  static const String failed = 'Failed';

  /// The label to show for [status].
  static String label(String status) => switch (status) {
        done => 'Acesso concedido',
        failed => 'Acesso falhou',
        _ => 'Acesso pendente',
      };
}

/// The labels for the platform products a tenant can enable.
///
/// The codes themselves live in `TenantProducts` (`rufino_core`) because both
/// products and the shell read them; only the naming is this module's.
abstract final class TenantProductLabels {
  /// The label to show for [product], or the raw code when unknown — an
  /// unknown code is a new product, not a reason to render an empty chip.
  static String label(String product) => switch (product) {
        'PeopleManagement' => 'Gestão de Pessoas',
        'BillPayment' => 'Contas a Pagar',
        _ => product,
      };
}
