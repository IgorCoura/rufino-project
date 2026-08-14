/// Identidade do cliente da plataforma: o seletor de contexto por onde o app
/// entra, e o back-office que cadastra e mantém os tenants.
///
/// A casca do app enxerga este módulo por um contrato pequeno — rotas,
/// providers e entradas de menu —, e nada além dele. Quem só precisa saber
/// qual tenant está selecionado lê o `TenantContext` de `rufino_core`, sem
/// depender deste pacote.
library;

export 'src/data/tenant_api_models.dart' show RegisterTenantInput;
export 'src/data/tenant_api_service.dart';
export 'src/data/tenant_repository_impl.dart';
export 'src/domain/my_tenant.dart';
export 'src/domain/tax_id.dart';
export 'src/domain/tenant.dart';
export 'src/domain/tenant_enums.dart';
export 'src/domain/tenant_exception.dart';
export 'src/domain/tenant_repository.dart';
export 'src/domain/tenant_summary.dart';
export 'src/tenant_permissions.dart';
export 'src/ui/create/tenant_form_screen.dart';
export 'src/ui/create/tenant_form_viewmodel.dart';
export 'src/ui/detail/tenant_detail_screen.dart';
export 'src/ui/detail/tenant_detail_viewmodel.dart';
export 'src/ui/list/tenant_list_screen.dart';
export 'src/ui/list/tenant_list_viewmodel.dart';
export 'src/ui/select/tenant_selection_screen.dart';
export 'src/ui/select/tenant_selection_viewmodel.dart';
export 'src/ui/tenant_routes.dart';
