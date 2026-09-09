import 'dart:typed_data';

/// The original document as it arrived, ready to be shown.
///
/// The app never caches it to disk: the artifact is the evidence of what the
/// system saw when it decided to pay, and a copy left behind by the app would
/// outlive the session that was allowed to see it.
///
/// Downloading is a different thing and is allowed: there the person asked for
/// the file, chose where it goes, and owns it from then on. What stays banned
/// is the app writing a copy nobody asked for.
class CapturedArtifact {
  /// Creates the artifact record.
  const CapturedArtifact({
    required this.bytes,
    required this.contentType,
    this.fileName,
  });

  /// The document's bytes.
  final Uint8List bytes;

  /// The media type the server served, already without parameters.
  final String contentType;

  /// The name the server suggested, when it sent one.
  final String? fileName;

  /// Whether this is a PDF — the common case.
  bool get isPdf => contentType == 'application/pdf';

  /// Whether this is an image, which the mailbox delivers more often than one
  /// would guess: a photo of the bank slip, or a scanned page.
  bool get isImage => contentType.startsWith('image/');

  /// Whether the viewer can render it at all.
  bool get isViewable => isPdf || isImage;
}

/// Devolve [fileName] com a extensao que [contentType] pede.
///
/// Espelha o `EnsureExtension` do servidor de proposito: o nome amigavel que a
/// tela monta ("comprovante-Fulano-2026-09-25") nao carrega extensao, e arquivo
/// sem extensao nao abre com dois cliques em sistema nenhum.
///
/// Tipo que o app nao conhece cai em [borrowFrom] — o nome que o servidor
/// sugeriu, que ja vem com a extensao certa. Chutar `.pdf` seria pior que nao
/// ter extensao; copiar a que o servidor mandou nao e chute.
String ensureExtension(String fileName, String contentType,
    {String? borrowFrom}) {
  final name = fileName.trim().isEmpty ? 'documento' : fileName.trim();
  final extension = _extensionFor(contentType) ?? _extensionOf(borrowFrom);

  if (extension == null || name.toLowerCase().endsWith(extension)) return name;
  return '$name$extension';
}

/// A extensao de um nome de arquivo, com o ponto — nula quando nao ha.
String? _extensionOf(String? fileName) {
  final dot = fileName?.lastIndexOf('.') ?? -1;
  if (fileName == null || dot <= 0 || dot == fileName.length - 1) return null;

  final extension = fileName.substring(dot).toLowerCase();
  return RegExp(r'^\.[a-z0-9]{1,8}$').hasMatch(extension) ? extension : null;
}

String? _extensionFor(String contentType) =>
    switch (contentType.split(';').first.trim().toLowerCase()) {
      'application/pdf' => '.pdf',
      'image/jpeg' => '.jpg',
      'image/png' => '.png',
      'image/webp' => '.webp',
      'image/heic' => '.heic',
      'text/html' => '.html',
      _ => null,
    };

/// Monta o nome com que um documento de boleto deve ser salvo.
///
/// `comprovante-SECONCI-SP-2026-09-25` em vez de `comprovante-{guid}`: o nome
/// tecnico do servidor identifica o registro, nao o pagamento, e uma pasta de
/// downloads com meia duzia deles obriga a abrir um por um.
///
/// Devolve `null` quando nao ha beneficiario NEM vencimento — sem nenhum dos
/// dois o nome "amigavel" seria so o prefixo repetido, pior que o do servidor,
/// que ao menos distingue um arquivo do outro.
String? suggestedDocumentFileName({
  required String prefix,
  String? beneficiary,
  DateTime? dueDate,
}) {
  final parts = <String>[
    prefix,
    if (beneficiary != null) _slug(beneficiary),
    if (dueDate != null) _isoDate(dueDate),
  ]..removeWhere((p) => p.isEmpty);

  return parts.length > 1 ? parts.join('-') : null;
}

/// Reduz um nome proprio ao que sobrevive a qualquer sistema de arquivos.
///
/// Acento vira a letra simples, o resto que nao for letra, digito ou hifen
/// vira hifen, e a corrida de hifens colapsa. O corte em 60 existe porque
/// razao social e longa e o caminho inteiro tem teto no Windows.
String _slug(String value) {
  const withAccent = 'áàâãäéèêëíìîïóòôõöúùûüçÁÀÂÃÄÉÈÊËÍÌÎÏÓÒÔÕÖÚÙÛÜÇ';
  const withoutAccent = 'aaaaaeeeeiiiiooooouuuucAAAAAEEEEIIIIOOOOOUUUUC';

  final buffer = StringBuffer();
  for (final rune in value.trim().runes) {
    final char = String.fromCharCode(rune);
    final index = withAccent.indexOf(char);
    buffer.write(index >= 0 ? withoutAccent[index] : char);
  }

  final slug = buffer
      .toString()
      .replaceAll(RegExp('[^A-Za-z0-9-]+'), '-')
      .replaceAll(RegExp('-+'), '-')
      .replaceAll(RegExp(r'^-|-$'), '');

  return slug.length > 60 ? slug.substring(0, 60) : slug;
}

String _isoDate(DateTime date) =>
    '${date.year.toString().padLeft(4, '0')}-'
    '${date.month.toString().padLeft(2, '0')}-'
    '${date.day.toString().padLeft(2, '0')}';
