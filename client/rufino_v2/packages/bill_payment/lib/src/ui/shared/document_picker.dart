/// Um documento escolhido pela pessoa no seletor de arquivos do sistema.
typedef PickedDocument = ({List<int> bytes, String fileName, String contentType});

/// Abre o seletor e devolve o que foi escolhido — nulo quando a pessoa desiste.
typedef DocumentPicker = Future<PickedDocument?> Function();

/// Abre um endereco no navegador do sistema. `false` quando nao foi possivel.
///
/// Implementado pela casca, como o seletor de arquivos: abrir navegador e
/// capacidade de plataforma (`url_launcher`), e o modulo nao carrega plugin.
typedef LinkOpener = Future<bool> Function(String url);
