/// Os nomes de recurso e escopo que este produto usa no Keycloak.
///
/// Existem para que ninguém escreva a string à mão: o nome errado não falha na
/// compilação nem no teste — o guard simplesmente esconde o botão, e ninguém
/// descobre até alguém reclamar que não consegue fazer algo.
///
/// **Não há `PeopleManagementPermissionNotifier` aqui, e isso é deliberado.**
/// O `provider` resolve por tipo, então `bill_payment` e `tenant_management`
/// precisaram de uma subclasse de [PermissionNotifier] para não disputarem a
/// mesma entrada na árvore. Este produto é o dono do tipo base — a audiência
/// `people-management-api` é a que o notifier sem parâmetro atende —, então uma
/// subclasse aqui não resolveria colisão nenhuma e obrigaria a trocar os 55
/// guards que hoje resolvem para a base.
library;

/// Recursos do realm, em kebab-case minúsculo.
abstract final class PeopleManagementResources {
  /// Cadastro da empresa (o `Company` local, não o tenant).
  static const String company = 'company';

  /// Ferramentas de diagnóstico.
  static const String debug = 'debug';

  /// Departamentos.
  static const String department = 'department';

  /// Documentos e suas unidades.
  static const String document = 'document';

  /// Grupos de documento.
  static const String documentGroup = 'document-group';

  /// Modelos de documento.
  static const String documentTemplate = 'document-template';

  /// Funcionários.
  static const String employee = 'employee';

  /// Cargos.
  static const String position = 'position';

  /// Matriz de documentos exigidos.
  static const String requireDocuments = 'require-documents';

  /// Funções.
  static const String role = 'role';

  /// Locais de trabalho.
  static const String workplace = 'workplace';
}

/// Escopos do realm.
abstract final class PeopleManagementScopes {
  /// Ler.
  static const String view = 'view';

  /// Cadastrar.
  static const String create = 'create';

  /// Editar.
  static const String edit = 'edit';

  /// Enviar arquivo.
  static const String upload = 'upload';

  /// Baixar arquivo.
  static const String download = 'download';

  /// Mandar para assinatura.
  static const String sendToSign = 'send2sign';

  /// Gerar documento.
  static const String generate = 'generate';

  /// Receber retorno do provedor de assinatura.
  static const String webhook = 'webhook';
}
