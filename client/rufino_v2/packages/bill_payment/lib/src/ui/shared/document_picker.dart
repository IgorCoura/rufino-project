import 'dart:typed_data';

/// Um documento escolhido pela pessoa no seletor de arquivos do sistema.
typedef PickedDocument = ({List<int> bytes, String fileName, String contentType});

/// Abre o seletor e devolve o que foi escolhido — nulo quando a pessoa desiste.
typedef DocumentPicker = Future<PickedDocument?> Function();

/// Abre um endereco no navegador do sistema. `false` quando nao foi possivel.
///
/// Implementado pela casca, como o seletor de arquivos: abrir navegador e
/// capacidade de plataforma (`url_launcher`), e o modulo nao carrega plugin.
typedef LinkOpener = Future<bool> Function(String url);

/// Salva [bytes] no dispositivo sob [fileName]. `false` quando a pessoa desistiu.
///
/// Terceira capacidade de plataforma que a casca empresta ao modulo, pelo
/// mesmo motivo das duas de cima: salvar arquivo e plugin (`file_saver` no
/// desktop e no celular, download por blob no navegador), e declara-lo aqui
/// obrigaria todo consumidor do modulo a carrega-lo sem usar.
///
/// O `false` distingue "a pessoa fechou a caixa de dialogo" de "salvou" — sem
/// ele a tela anunciaria sucesso para quem desistiu. Falha de verdade sobe
/// como excecao, e quem a traduz e o ViewModel.
///
/// [fileName] ja vem com extensao.
typedef DocumentSaver = Future<bool> Function({
  required String fileName,
  required Uint8List bytes,
});
