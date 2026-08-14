/// Reexporta os guards que agora vivem em `rufino_core`.
///
/// Eles ganharam parâmetro de tipo (o notifier da audiência). Omitir o tipo
/// resolve para `PermissionNotifier`, que é a audiência do PeopleManagement —
/// por isso os pontos de uso deste app não mudaram.
///
/// Código novo deve importar `package:rufino_core/rufino_core.dart`.
library;
export 'package:rufino_core/rufino_core.dart' show ModuleGuard, PermissionGuard;
