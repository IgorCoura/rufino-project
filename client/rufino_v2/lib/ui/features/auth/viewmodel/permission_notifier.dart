/// Reexporta o notifier que agora vive em `rufino_core`.
///
/// Cada audiência do Keycloak tem o seu: este é o do `people-management-api`,
/// e o `tenant-management-api` usa uma subclasse registrada com o próprio
/// tipo, para que `provider` distinga os dois.
///
/// Código novo deve importar `package:rufino_core/rufino_core.dart`.
library;
export 'package:rufino_core/rufino_core.dart'
    show PermissionNotifier, PermissionStatus;
