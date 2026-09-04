import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_core/rufino_core.dart';
import 'package:tenant_management/tenant_management.dart';

TenantMembership _membership({
  String email = 'dono@empresa.com.br',
  String role = MembershipRoles.owner,
  bool isActive = true,
  String provisioning = ProvisioningStatuses.done,
}) {
  return TenantMembership(
    email: email,
    role: role,
    isActive: isActive,
    provisioning: provisioning,
  );
}

Tenant _tenant({
  String kind = TenantKinds.company,
  String status = TenantStatuses.active,
  String provisioning = ProvisioningStatuses.done,
  String tradeName = 'Pão Quente',
  List<TenantMembership>? memberships,
  List<TenantProductInfo>? products,
}) {
  return Tenant(
    id: '11111111-1111-1111-1111-111111111111',
    kind: kind,
    legalName: 'Padaria do Zé LTDA',
    tradeName: tradeName,
    primaryTaxId: '11222333000181',
    status: status,
    suspensionReason: '',
    accessProvisioning: provisioning,
    contact: const TenantContact(
      email: 'contato@paoquente.com.br',
      phone: '31999990000',
    ),
    address: const TenantAddress(
      zipCode: '30110000',
      street: 'Rua das Flores',
      number: '100',
      complement: '',
      neighborhood: 'Centro',
      city: 'Belo Horizonte',
      state: 'MG',
    ),
    products: products ??
        [
          TenantProductInfo(
            product: 'PeopleManagement',
            isActive: true,
            activatedAt: DateTime(2026, 1, 1),
          ),
        ],
    memberships: memberships ?? [_membership()],
    createdAt: DateTime(2026, 1, 1),
    updatedAt: DateTime(2026, 1, 2),
  );
}

void main() {
  group('Tenant', () {
    test('prefers the trade name and falls back to the legal one', () {
      expect(_tenant().displayName, 'Pão Quente');
      expect(_tenant(tradeName: '').displayName, 'Padaria do Zé LTDA');
    });

    test('labels the document field by the kind of customer', () {
      expect(_tenant().taxIdLabel, 'CNPJ');
      expect(_tenant(kind: TenantKinds.individual).taxIdLabel, 'CPF');
    });

    test('flags a cadastro whose access never reached the provider', () {
      final failed = _tenant(provisioning: ProvisioningStatuses.failed);

      expect(failed.hasFailedProvisioning, isTrue);
      expect(failed.needsReprovisioning, isTrue);
      expect(_tenant().needsReprovisioning, isFalse);
    });

    test('a pending grant is also worth reprovisioning', () {
      expect(
        _tenant(provisioning: ProvisioningStatuses.pending)
            .needsReprovisioning,
        isTrue,
      );
    });

    test('reports which products are on', () {
      expect(_tenant().hasProduct('PeopleManagement'), isTrue);
      expect(_tenant().hasProduct('BillPayment'), isFalse);
    });

    test('a deactivated product is not an active one', () {
      final tenant = _tenant(
        products: [
          TenantProductInfo(
            product: 'BillPayment',
            isActive: false,
            activatedAt: DateTime(2026, 1, 1),
            deactivatedAt: DateTime(2026, 2, 1),
          ),
        ],
      );

      expect(tenant.hasProduct('BillPayment'), isFalse);
      expect(tenant.activeProducts, isEmpty);
    });

    test('the last responsible person cannot be revoked', () {
      final tenant = _tenant();
      final owner = tenant.memberships.first;

      expect(tenant.canRevoke(owner), isFalse);
    });

    test('a responsible person can be revoked when there is another one', () {
      final tenant = _tenant(
        memberships: [
          _membership(),
          _membership(email: 'socio@empresa.com.br'),
        ],
      );

      expect(tenant.canRevoke(tenant.memberships.first), isTrue);
    });

    test('a revoked owner does not count as one of the responsible people',
        () {
      final tenant = _tenant(
        memberships: [
          _membership(),
          _membership(email: 'ex@empresa.com.br', isActive: false),
        ],
      );

      expect(tenant.canRevoke(tenant.memberships.first), isFalse);
      expect(tenant.canRevoke(tenant.memberships.last), isFalse);
    });

    test('an ordinary member can always be revoked', () {
      final tenant = _tenant(
        memberships: [
          _membership(),
          _membership(
            email: 'membro@empresa.com.br',
            role: MembershipRoles.member,
          ),
        ],
      );

      expect(tenant.canRevoke(tenant.memberships.last), isTrue);
    });

    test('a suspended cadastro says so', () {
      expect(_tenant(status: TenantStatuses.suspended).isSuspended, isTrue);
      expect(_tenant().isSuspended, isFalse);
    });
  });

  group('Tenant validators', () {
    test('the legal name is required and capped at 200 characters', () {
      expect(Tenant.validateLegalName(''), isNotNull);
      expect(Tenant.validateLegalName('   '), isNotNull);
      expect(Tenant.validateLegalName('a' * 201), isNotNull);
      expect(Tenant.validateLegalName('Padaria do Zé'), isNull);
    });

    test('a natural person may not have a trade name', () {
      expect(
        Tenant.validateTradeName(TenantKinds.individual, 'Pão Quente'),
        isNotNull,
      );
      expect(Tenant.validateTradeName(TenantKinds.individual, ''), isNull);
      expect(
        Tenant.validateTradeName(TenantKinds.company, 'Pão Quente'),
        isNull,
      );
    });

    test('the document is validated against the kind of customer', () {
      expect(
        Tenant.validateTaxId(TenantKinds.company, '11222333000181'),
        isNull,
      );
      expect(
        Tenant.validateTaxId(TenantKinds.company, '52998224725'),
        'CNPJ inválido.',
      );
      expect(
        Tenant.validateTaxId(TenantKinds.individual, '52998224725'),
        isNull,
      );
      expect(Tenant.validateTaxId(TenantKinds.individual, ''), isNotNull);
    });

    test('the e-mail is required and must look like one', () {
      expect(Tenant.validateEmail(''), isNotNull);
      expect(Tenant.validateEmail('nao-e-email'), 'E-mail inválido.');
      expect(Tenant.validateEmail('contato@empresa.com.br'), isNull);
    });

    test('the phone is optional but must have 10 or 11 digits when given', () {
      expect(Tenant.validatePhone(''), isNull);
      expect(Tenant.validatePhone('(31) 99999-0000'), isNull);
      expect(Tenant.validatePhone('(31) 9999-0000'), isNull);
      expect(Tenant.validatePhone('319'), 'Telefone inválido.');
    });

    test('the CEP needs exactly eight digits', () {
      expect(Tenant.validateZipCode('30110-000'), isNull);
      expect(Tenant.validateZipCode('3011000'), 'CEP inválido.');
      expect(Tenant.validateZipCode(''), isNotNull);
    });

    test('the state is two letters', () {
      expect(Tenant.validateState('MG'), isNull);
      expect(Tenant.validateState('MGS'), 'UF inválida.');
    });

    test('a suspension needs a reason, capped at 300 characters', () {
      expect(Tenant.validateSuspensionReason(''), isNotNull);
      expect(Tenant.validateSuspensionReason('a' * 301), isNotNull);
      expect(Tenant.validateSuspensionReason('Inadimplência'), isNull);
    });
  });

  group('TenantAddress', () {
    test('keeps number and complement when a CEP lookup fills the rest', () {
      const typed = TenantAddress(
        zipCode: '30110000',
        street: '',
        number: '100',
        complement: 'Sala 2',
        neighborhood: '',
        city: '',
        state: '',
      );

      final filled = typed.fillFrom(
        const CepLookup(
          zipCode: '30110-000',
          street: 'Rua das Flores',
          complement: '',
          neighborhood: 'Centro',
          city: 'Belo Horizonte',
          state: 'MG',
        ),
      );

      expect(filled.street, 'Rua das Flores');
      expect(filled.city, 'Belo Horizonte');
      expect(filled.number, '100');
      expect(filled.complement, 'Sala 2');
    });

    test('formats the CEP and leaves an incomplete one alone', () {
      expect(
        const TenantAddress(
          zipCode: '30110000',
          street: '',
          number: '',
          complement: '',
          neighborhood: '',
          city: '',
          state: '',
        ).formattedZipCode,
        '30110-000',
      );
    });
  });

  group('MyTenant', () {
    test('a suspended tenant is shown but cannot be entered', () {
      const suspended = MyTenant(
        id: 'x',
        kind: TenantKinds.company,
        legalName: 'Mercado Central ME',
        tradeName: '',
        status: TenantStatuses.suspended,
        role: MembershipRoles.owner,
        activeProducts: [],
      );

      expect(suspended.isSuspended, isTrue);
      expect(suspended.isSelectable, isFalse);
    });

    test('becomes the app-wide selection without losing a field', () {
      const entry = MyTenant(
        id: 'x',
        kind: TenantKinds.company,
        legalName: 'Padaria do Zé LTDA',
        tradeName: 'Pão Quente',
        status: TenantStatuses.active,
        role: MembershipRoles.owner,
        activeProducts: ['PeopleManagement'],
      );

      final selected = entry.toSelectedTenant();

      expect(selected.id, entry.id);
      expect(selected.displayName, 'Pão Quente');
      expect(selected.hasProduct('PeopleManagement'), isTrue);
      expect(selected.isOwner, isTrue);
    });
  });
}
