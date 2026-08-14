/// `Permission` mudou para `rufino_core` quando o app passou a consultar duas
/// audiências do Keycloak — o modelo de autorização é o mesmo nos três
/// produtos, então ele é fundação, não código de gestão de pessoas.
///
/// Este arquivo permanece só para não obrigar os pontos de uso a trocar de
/// import. Código novo deve importar `package:rufino_core/rufino_core.dart`.
library;
export 'package:rufino_core/rufino_core.dart' show Permission;
