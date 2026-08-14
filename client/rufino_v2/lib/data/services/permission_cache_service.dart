/// Reexporta o cache que agora vive em `rufino_core`.
///
/// A chave é parâmetro do construtor: duas audiências dividindo a mesma
/// sobrescreveriam uma à outra.
///
/// Código novo deve importar `package:rufino_core/rufino_core.dart`.
library;
export 'package:rufino_core/rufino_core.dart' show PermissionCacheService;
