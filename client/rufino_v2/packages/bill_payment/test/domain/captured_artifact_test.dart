import 'package:bill_payment/bill_payment.dart';
import 'package:flutter_test/flutter_test.dart';

/// O nome com que o documento chega à pasta de downloads.
///
/// O nome técnico do servidor (`boleto-{guid}.pdf`) identifica o registro, não
/// o pagamento — meia dúzia deles numa pasta obriga a abrir um por um.
void main() {
  group('suggestedDocumentFileName', () {
    // Beneficiário e vencimento juntos: é o nome que a pessoa reconhece.
    test('joins the prefix, the beneficiary and the due date', () {
      final name = suggestedDocumentFileName(
        prefix: 'comprovante',
        beneficiary: 'SECONCI-SP',
        dueDate: DateTime(2026, 9, 25),
      );

      expect(name, 'comprovante-SECONCI-SP-2026-09-25');
    });

    // A data sai em ISO para os arquivos ordenarem sozinhos na pasta.
    test('pads month and day so the folder sorts by date', () {
      final name = suggestedDocumentFileName(
        prefix: 'boleto',
        beneficiary: 'Enel',
        dueDate: DateTime(2026, 3, 7),
      );

      expect(name, 'boleto-Enel-2026-03-07');
    });

    // Razão social real tem acento, ponto, barra e vírgula — nada disso
    // sobrevive num nome de arquivo em todos os sistemas.
    test('reduces a legal name to what a file system accepts', () {
      final name = suggestedDocumentFileName(
        prefix: 'boleto',
        beneficiary: 'SABESP — Cia. de Saneamento Básico S/A',
        dueDate: DateTime(2026, 1, 10),
      );

      expect(name, 'boleto-SABESP-Cia-de-Saneamento-Basico-S-A-2026-01-10');
    });

    // Um dos dois basta: quem tem só o vencimento ainda distingue um arquivo
    // do outro, que é o ponto.
    test('accepts having only one of the two', () {
      expect(
        suggestedDocumentFileName(
          prefix: 'boleto',
          dueDate: DateTime(2026, 5, 2),
        ),
        'boleto-2026-05-02',
      );
      expect(
        suggestedDocumentFileName(prefix: 'boleto', beneficiary: 'Enel'),
        'boleto-Enel',
      );
    });

    // Sem nenhum dos dois o nome seria só o prefixo, repetido em todo arquivo
    // — pior que o do servidor, que ao menos os distingue. Nulo cede a vez.
    test('gives up when it has neither, so the server name wins', () {
      expect(suggestedDocumentFileName(prefix: 'boleto'), isNull);
      expect(
        suggestedDocumentFileName(prefix: 'boleto', beneficiary: '   '),
        isNull,
      );
    });

    // Razão social longa não pode estourar o teto de caminho do Windows.
    test('caps a very long beneficiary', () {
      final name = suggestedDocumentFileName(
        prefix: 'boleto',
        beneficiary: 'A' * 200,
        dueDate: DateTime(2026, 1, 1),
      );

      expect(name!.length, lessThan(90));
    });
  });

  group('ensureExtension', () {
    // O nome amigável não carrega extensão, e arquivo sem extensão não abre
    // com dois cliques em sistema nenhum.
    test('appends the extension the media type asks for', () {
      expect(
        ensureExtension('comprovante-Enel-2026-09-25', 'application/pdf'),
        'comprovante-Enel-2026-09-25.pdf',
      );
      expect(ensureExtension('foto', 'image/jpeg'), 'foto.jpg');
    });

    // O nome do servidor já vem com extensão — acrescentar de novo daria
    // "boleto.pdf.pdf".
    test('does not double an extension that is already there', () {
      expect(ensureExtension('boleto.pdf', 'application/pdf'), 'boleto.pdf');
      expect(ensureExtension('BOLETO.PDF', 'application/pdf'), 'BOLETO.PDF');
    });

    // Tipo desconhecido devolve o nome intacto: cravar `.pdf` sobre o que não
    // é PDF seria pior que ficar sem extensão.
    test('leaves the name alone for a media type it does not know', () {
      expect(
        ensureExtension('documento', 'application/octet-stream'),
        'documento',
      );
    });

    // O parâmetro do cabeçalho não muda o tipo.
    test('ignores the content type parameters', () {
      expect(
        ensureExtension('documento', 'application/pdf; charset=utf-8'),
        'documento.pdf',
      );
    });

    // Nome vazio ainda precisa virar arquivo.
    test('names the file when there is no name at all', () {
      expect(ensureExtension('   ', 'application/pdf'), 'documento.pdf');
    });
  });
}
