/// Fundação compartilhada entre os produtos do app Rufino.
///
/// Exporta apenas o que serve a mais de um produto. Nada específico de gestão
/// de pessoas ou de contas a pagar atravessa este barril — o critério está no
/// `pubspec.yaml` deste pacote.
library;

export 'src/auth/auth_exception.dart';
export 'src/auth/permission.dart';
export 'src/auth/permission_api_service.dart';
export 'src/auth/permission_cache_service.dart';
export 'src/auth/permission_exception.dart';
export 'src/auth/permission_guard.dart';
export 'src/auth/permission_notifier.dart';
export 'src/auth/permission_repository.dart';
export 'src/cep/cep_api_service.dart';
export 'src/cep/cep_exception.dart';
export 'src/expected_failure.dart';
export 'src/http_exception.dart';
export 'src/module/app_module.dart';
export 'src/module/home_entry.dart';
export 'src/monitoring/error_reporter.dart';
export 'src/monitoring/noop_error_reporter.dart';
export 'src/monitoring/pii_scrubber.dart';
export 'src/network/api_status.dart';
export 'src/network/session_aware_http_client.dart';
export 'src/result.dart';
export 'src/storage/secure_storage.dart';
export 'src/tenant/product_guard.dart';
export 'src/tenant/selected_tenant.dart';
export 'src/tenant/tenant_context.dart';
export 'src/theme/app_breakpoints.dart';
export 'src/theme/app_colors.dart';
export 'src/theme/app_spacing.dart';
export 'src/theme/app_theme.dart';
export 'src/theme/theme_notifier.dart';
export 'src/ui/section_card.dart';
