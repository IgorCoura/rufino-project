import 'package:rufino_core/rufino_core.dart';

import 'tax_id.dart';
import 'tenant_enums.dart';

/// A tenant's postal address.
///
/// Mandatory in the cadastro: the payment provider requires it for both
/// natural persons and companies, so leaving it optional would only postpone
/// the discovery to the day money has to move.
class TenantAddress {
  /// Creates the address.
  const TenantAddress({
    required this.zipCode,
    required this.street,
    required this.number,
    required this.complement,
    required this.neighborhood,
    required this.city,
    required this.state,
    this.country = 'BRASIL',
  });

  /// An empty address, for a form that has nothing to start from.
  static const TenantAddress empty = TenantAddress(
    zipCode: '',
    street: '',
    number: '',
    complement: '',
    neighborhood: '',
    city: '',
    state: '',
  );

  /// Postal code (CEP), digits only or formatted.
  final String zipCode;

  /// Street name.
  final String street;

  /// Street number.
  final String number;

  /// Complement — the only optional field.
  final String complement;

  /// Neighborhood.
  final String neighborhood;

  /// City.
  final String city;

  /// Two-letter state code.
  final String state;

  /// Country, defaulted by the backend to `BRASIL`.
  final String country;

  /// One-line rendering for view mode.
  String get singleLine {
    final head = [street, number].where((p) => p.isNotEmpty).join(', ');
    final withComplement =
        complement.isEmpty ? head : '$head — $complement';
    final tail = [neighborhood, city, state]
        .where((p) => p.isNotEmpty)
        .join(' / ');
    return [withComplement, tail, formattedZipCode]
        .where((p) => p.isNotEmpty)
        .join(' · ');
  }

  /// The CEP formatted as `00000-000`, or unchanged when incomplete.
  String get formattedZipCode {
    final d = zipCode.replaceAll(RegExp(r'[^\d]'), '');
    if (d.length != 8) return zipCode;
    return '${d.substring(0, 5)}-${d.substring(5)}';
  }

  /// Returns a copy with the given fields replaced.
  TenantAddress copyWith({
    String? zipCode,
    String? street,
    String? number,
    String? complement,
    String? neighborhood,
    String? city,
    String? state,
    String? country,
  }) {
    return TenantAddress(
      zipCode: zipCode ?? this.zipCode,
      street: street ?? this.street,
      number: number ?? this.number,
      complement: complement ?? this.complement,
      neighborhood: neighborhood ?? this.neighborhood,
      city: city ?? this.city,
      state: state ?? this.state,
      country: country ?? this.country,
    );
  }

  /// Fills street, neighborhood, city and state from a CEP lookup, keeping
  /// what the user already typed for number and complement.
  TenantAddress fillFrom(CepLookup lookup) {
    return copyWith(
      zipCode: lookup.zipCode.isEmpty ? zipCode : lookup.zipCode,
      street: lookup.street,
      neighborhood: lookup.neighborhood,
      city: lookup.city,
      state: lookup.state,
    );
  }
}

/// A tenant's contact channel.
class TenantContact {
  /// Creates the contact.
  const TenantContact({required this.email, required this.phone});

  /// E-mail — mandatory.
  final String email;

  /// Phone — optional, 10 or 11 digits.
  final String phone;

  /// Whether a phone was informed.
  bool get hasPhone => phone.trim().isNotEmpty;

  /// The phone formatted as `(00) 00000-0000`, or unchanged when incomplete.
  String get formattedPhone {
    final d = phone.replaceAll(RegExp(r'[^\d]'), '');
    if (d.length == 11) {
      return '(${d.substring(0, 2)}) ${d.substring(2, 7)}-${d.substring(7)}';
    }
    if (d.length == 10) {
      return '(${d.substring(0, 2)}) ${d.substring(2, 6)}-${d.substring(6)}';
    }
    return phone;
  }
}

/// Somebody's access to a tenant.
///
/// Keyed by **e-mail**, not by the person's id at the identity provider: when
/// access is granted the person may not exist there yet, and it is the
/// provisioning that brings the id back.
class TenantMembership {
  /// Creates the membership.
  const TenantMembership({
    required this.email,
    required this.role,
    required this.isActive,
    required this.provisioning,
    this.userId,
  });

  /// The person's e-mail — the natural key of the grant.
  final String email;

  /// `Owner` or `Member`.
  final String role;

  /// Whether the grant is currently in force. Revoking keeps the row.
  final bool isActive;

  /// State of the grant at the identity provider.
  final String provisioning;

  /// The person's id at the identity provider, once it exists.
  final String? userId;

  /// Whether this person answers for the tenant.
  bool get isOwner => role == MembershipRoles.owner;

  /// Whether the grant has not reached the identity provider yet.
  bool get isPending => provisioning == ProvisioningStatuses.pending;

  /// Whether the grant failed to reach the identity provider.
  bool get hasFailed => provisioning == ProvisioningStatuses.failed;

  /// The role label to show.
  String get roleLabel => MembershipRoles.label(role);

  /// The provisioning label to show.
  String get provisioningLabel => ProvisioningStatuses.label(provisioning);
}

/// A product enabled — or once enabled — for a tenant.
class TenantProductInfo {
  /// Creates the product record.
  const TenantProductInfo({
    required this.product,
    required this.isActive,
    required this.activatedAt,
    this.deactivatedAt,
  });

  /// The product code.
  final String product;

  /// Whether the product is on right now.
  final bool isActive;

  /// When it was turned on.
  final DateTime activatedAt;

  /// When it was turned off, when it was.
  final DateTime? deactivatedAt;

  /// The product label to show.
  String get label => TenantProductLabels.label(product);
}

/// The platform's customer — a natural person or a company, same model.
///
/// The difference between the two lives in exactly two places: the kind of
/// document the cadastro requires and the right to a trade name. Nothing
/// else in this class may branch on [kind].
class Tenant {
  /// Creates the tenant.
  const Tenant({
    required this.id,
    required this.kind,
    required this.legalName,
    required this.tradeName,
    required this.primaryTaxId,
    required this.status,
    required this.suspensionReason,
    required this.accessProvisioning,
    required this.contact,
    required this.address,
    required this.products,
    required this.memberships,
    required this.createdAt,
    required this.updatedAt,
  });

  /// The tenant id — the same value the products carry in route and claim.
  final String id;

  /// `Individual` or `Company`.
  final String kind;

  /// Civil name (individual) or corporate name (company).
  final String legalName;

  /// Trade name. Always empty for an individual, by invariant.
  final String tradeName;

  /// CPF or CNPJ, as the backend stores it.
  final String primaryTaxId;

  /// `Active` or `Suspended`.
  final String status;

  /// Why it was suspended. Empty while active.
  final String suspensionReason;

  /// Aggregated state of the access grants at the identity provider.
  final String accessProvisioning;

  /// The tenant's contact channel.
  final TenantContact contact;

  /// The tenant's address.
  final TenantAddress address;

  /// Every product record, active or not.
  final List<TenantProductInfo> products;

  /// Every access grant, in force or revoked.
  final List<TenantMembership> memberships;

  /// When the cadastro was created.
  final DateTime createdAt;

  /// When it last changed.
  final DateTime updatedAt;

  /// Whether this tenant is a natural person.
  bool get isIndividual => kind == TenantKinds.individual;

  /// Whether the cadastro is frozen. A suspended tenant refuses every change.
  bool get isSuspended => status == TenantStatuses.suspended;

  /// Whether an access grant failed to reach the identity provider.
  ///
  /// This is the reason the back-office exists: the cadastro can be complete
  /// and the customer still locked out.
  bool get hasFailedProvisioning =>
      accessProvisioning == ProvisioningStatuses.failed;

  /// Whether some grant is still on its way to the identity provider.
  bool get hasPendingProvisioning =>
      accessProvisioning == ProvisioningStatuses.pending;

  /// Whether anything is worth reprovisioning.
  bool get needsReprovisioning =>
      hasFailedProvisioning || hasPendingProvisioning;

  /// The best name to show.
  String get displayName => tradeName.isNotEmpty ? tradeName : legalName;

  /// The document formatted for reading.
  String get formattedTaxId => TaxId.format(primaryTaxId);

  /// The label of the document field for this kind of tenant.
  String get taxIdLabel => isIndividual ? 'CPF' : 'CNPJ';

  /// The grants currently in force.
  List<TenantMembership> get activeMemberships =>
      memberships.where((m) => m.isActive).toList();

  /// The products currently on.
  List<TenantProductInfo> get activeProducts =>
      products.where((p) => p.isActive).toList();

  /// Whether [product] is on for this tenant.
  bool hasProduct(String product) =>
      products.any((p) => p.product == product && p.isActive);

  /// Whether [membership] may be revoked.
  ///
  /// The last responsible person cannot lose access (`TNM.TNT20`) — the
  /// server refuses it, and offering the button anyway would be offering a
  /// dead end.
  bool canRevoke(TenantMembership membership) {
    if (!membership.isActive) return false;
    if (!membership.isOwner) return true;
    return activeMemberships.where((m) => m.isOwner).length > 1;
  }

  // ─── Validators ────────────────────────────────────────────────────────

  /// Validates a required text field.
  static String? validateRequired(String? value) {
    if (value == null || value.trim().isEmpty) return 'Não pode ser vazio.';
    return null;
  }

  /// Validates the legal name: required, up to 200 characters.
  static String? validateLegalName(String? value) {
    final required = validateRequired(value);
    if (required != null) return required;
    if (value!.trim().length > 200) return 'Máximo de 200 caracteres.';
    return null;
  }

  /// Validates the trade name for [kind].
  ///
  /// A natural person has a name and nothing else: filling a trade name for
  /// one is refused here for the same reason the domain refuses it.
  static String? validateTradeName(String kind, String? value) {
    final text = value?.trim() ?? '';
    if (kind == TenantKinds.individual && text.isNotEmpty) {
      return 'Pessoa física não tem nome fantasia.';
    }
    if (text.length > 200) return 'Máximo de 200 caracteres.';
    return null;
  }

  /// Validates the primary document for [kind].
  static String? validateTaxId(String kind, String? value) {
    final required = validateRequired(value);
    if (required != null) return required;
    if (TaxId.isValidFor(kind, value!)) return null;
    return kind == TenantKinds.individual ? 'CPF inválido.' : 'CNPJ inválido.';
  }

  /// Validates a required e-mail field.
  static String? validateEmail(String? value) {
    final required = validateRequired(value);
    if (required != null) return required;
    final regex = RegExp(r'^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$');
    if (!regex.hasMatch(value!.trim())) return 'E-mail inválido.';
    return null;
  }

  /// Validates an optional phone: empty, or 10 to 11 digits.
  static String? validatePhone(String? value) {
    final text = value?.trim() ?? '';
    if (text.isEmpty) return null;
    final digits = text.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length < 10 || digits.length > 11) return 'Telefone inválido.';
    return null;
  }

  /// Validates a CEP: required, exactly 8 digits.
  static String? validateZipCode(String? value) {
    final required = validateRequired(value);
    if (required != null) return required;
    final digits = value!.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != 8) return 'CEP inválido.';
    return null;
  }

  /// Validates a state: required, exactly two letters.
  static String? validateState(String? value) {
    final required = validateRequired(value);
    if (required != null) return required;
    if (value!.trim().length != 2) return 'UF inválida.';
    return null;
  }

  /// Validates a suspension reason: required, up to 300 characters.
  static String? validateSuspensionReason(String? value) {
    final required = validateRequired(value);
    if (required != null) return required;
    if (value!.trim().length > 300) return 'Máximo de 300 caracteres.';
    return null;
  }
}
