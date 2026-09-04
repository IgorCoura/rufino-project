import 'employee_export_row.dart';

/// Pure helpers that turn a list of [SocRow]s into the columns and rows of
/// the "Planilha SOC" spreadsheet.
///
/// The column order matches the SOC import template exactly, including the
/// empty columns whose data is not yet captured by the system — those are
/// emitted as empty strings to preserve column alignment.
class SocXlsxBuilder {
  SocXlsxBuilder._();

  /// Display name of the worksheet inside the workbook.
  static const sheetName = 'SOC';

  /// File name (without extension) used when saving the spreadsheet.
  static const baseFileName = 'planilha_soc';

  /// The header row of the spreadsheet, in display order.
  static const headers = <String>[
    'Nome Unidade',
    'Nome Setor',
    'Nome Cargo',
    'Nome Funcionário',
    'Dt.Nascimento',
    'Sexo',
    'Situação',
    'Dt.Admissão',
    'Dt.Demissão',
    'Estado Civil',
    'Pis/Pasep',
    'Rg',
    'UF-RG',
    'CPF',
    'CBO',
    'GFIP',
    'Endereço Unidade',
    'Bairro Unidade',
    'Cidade Unidade',
    'Estado Unidade',
    'Cep Unidade',
    'CNPJ Unidade',
    'Tel1 Unidade',
    'Contato Unid',
    'Razão Social Unid.',
    'Dt. Ultima Movimentação',
    'CNAE 2.0',
    'Função',
    'Descrição Detalhada do Cargo',
    'Nº endereço Unidade',
    'Complemento endereço Unidade',
    'Regime de Revezamento',
    'Orgão Expedidor do RG',
    'Grau de Risco',
    'Data Emissão RG',
    'Origem Descrição Detalhada',
    'Código Categoria (eSocial)',
    'Matrícula RH',
  ];

  /// Returns the body rows in the order expected by [headers].
  static List<List<String>> toRows(List<SocRow> rows) {
    return rows
        .map(
          (row) => <String>[
            row.unitName,
            row.sectorName,
            row.positionName,
            row.employeeName,
            row.dateOfBirth,
            row.gender,
            row.status,
            row.admissionDate,
            row.dismissalDate,
            row.maritalStatus,
            row.pisPasep,
            row.rg,
            row.rgState,
            row.cpf,
            row.cbo,
            row.gfip,
            row.unitAddress,
            row.unitNeighborhood,
            row.unitCity,
            row.unitState,
            row.unitZipCode,
            row.unitCnpj,
            row.unitPhone,
            row.unitContact,
            row.unitCorporateName,
            row.lastMovementDate,
            row.cnae,
            row.role,
            row.positionDescription,
            row.unitAddressNumber,
            row.unitAddressComplement,
            row.shiftRegime,
            row.rgIssuer,
            row.riskLevel,
            row.rgIssueDate,
            row.descriptionOrigin,
            row.eSocialCategoryCode,
            row.hrRegistration,
          ],
        )
        .toList(growable: false);
  }
}
