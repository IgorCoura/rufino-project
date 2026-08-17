/// Splitting of a scanned page stack into several equally sized documents.
///
/// A single scanning pass often captures a pile of documents that all have
/// the same number of pages — one two-page form per employee, for instance.
/// Rather than scanning each one separately, the user scans the whole pile
/// and cuts it here, so the bulk pipeline sees one document per employee.
///
/// The split is only defined when every part comes out with the same number
/// of pages; a remainder means the pile was not what the user thought it
/// was, and guessing where the odd pages belong would silently attach a page
/// to the wrong employee.
library;

/// Splits [items] into consecutive parts of exactly [partSize] elements.
///
/// Parts follow the original order: with a part size of 2, the first part
/// holds elements 1 and 2, the second holds 3 and 4, and so on. Elements are
/// referenced, never copied, so splitting a stack of page bytes costs no
/// extra memory.
///
/// Requires `items.length` to be a positive multiple of [partSize] — check
/// the user's input with [validatePagesPerDocument] first.
List<List<T>> splitIntoEqualParts<T>(List<T> items, int partSize) {
  assert(partSize >= 1, 'partSize must be at least 1');
  assert(items.isNotEmpty, 'cannot split an empty list');
  assert(
    items.length % partSize == 0,
    'items.length (${items.length}) must be a multiple of partSize '
    '($partSize)',
  );

  return [
    for (var start = 0; start < items.length; start += partSize)
      items.sublist(start, start + partSize),
  ];
}

/// Form validator for the "páginas por documento" field of a split of
/// [totalPages] pages.
///
/// Returns `null` when [value] cuts the stack into equally sized documents,
/// or a message in Portuguese explaining why it does not.
String? validatePagesPerDocument(String? value, {required int totalPages}) {
  final text = value?.trim() ?? '';
  if (text.isEmpty) return 'Informe quantas páginas por documento.';

  final pagesPerDocument = int.tryParse(text);
  if (pagesPerDocument == null) return 'Informe um número inteiro.';
  if (pagesPerDocument < 1) return 'Mínimo de 1 página por documento.';
  if (pagesPerDocument > totalPages) {
    return 'A digitalização tem apenas $totalPages '
        '${totalPages == 1 ? 'página' : 'páginas'}.';
  }

  final remainder = totalPages % pagesPerDocument;
  if (remainder != 0) {
    return '$totalPages páginas não podem ser divididas a cada '
        '$pagesPerDocument (sobram $remainder).';
  }

  return null;
}
