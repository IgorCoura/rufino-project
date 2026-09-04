/// A single employee row of the "Planilha Etiquetas" spreadsheet.
class EtiquetaRow {
  /// Creates an [EtiquetaRow] with the five columns required by the labels
  /// printing flow.
  const EtiquetaRow({
    required this.name,
    required this.cpf,
    required this.position,
    required this.companyName,
    required this.cnpj,
  });

  /// The employee's full name.
  final String name;

  /// The employee's CPF formatted as `000.000.000-00`.
  final String cpf;

  /// The employee's position (cargo).
  final String position;

  /// The selected company's corporate name (Razão Social).
  final String companyName;

  /// The selected company's CNPJ formatted as `00.000.000/0000-00`.
  final String cnpj;
}

/// A single employee row of the "Planilha SOC" spreadsheet.
///
/// Every field is a [String] so missing data can be rendered as the empty
/// string without forcing nulls through the spreadsheet writer.
class SocRow {
  /// Creates a [SocRow] with all SOC columns. Defaults every field to the
  /// empty string so callers only need to fill in what is available.
  const SocRow({
    this.unitName = '',
    this.sectorName = '',
    this.positionName = '',
    this.employeeName = '',
    this.dateOfBirth = '',
    this.gender = '',
    this.status = '',
    this.admissionDate = '',
    this.dismissalDate = '',
    this.maritalStatus = '',
    this.pisPasep = '',
    this.rg = '',
    this.rgState = '',
    this.cpf = '',
    this.cbo = '',
    this.gfip = '',
    this.unitAddress = '',
    this.unitNeighborhood = '',
    this.unitCity = '',
    this.unitState = '',
    this.unitZipCode = '',
    this.unitCnpj = '',
    this.unitPhone = '',
    this.unitContact = '',
    this.unitCorporateName = '',
    this.lastMovementDate = '',
    this.cnae = '',
    this.role = '',
    this.positionDescription = '',
    this.unitAddressNumber = '',
    this.unitAddressComplement = '',
    this.shiftRegime = '',
    this.rgIssuer = '',
    this.riskLevel = '',
    this.rgIssueDate = '',
    this.descriptionOrigin = '',
    this.eSocialCategoryCode = '',
    this.hrRegistration = '',
  });

  final String unitName;
  final String sectorName;
  final String positionName;
  final String employeeName;
  final String dateOfBirth;
  final String gender;
  final String status;
  final String admissionDate;
  final String dismissalDate;
  final String maritalStatus;
  final String pisPasep;
  final String rg;
  final String rgState;
  final String cpf;
  final String cbo;
  final String gfip;
  final String unitAddress;
  final String unitNeighborhood;
  final String unitCity;
  final String unitState;
  final String unitZipCode;
  final String unitCnpj;
  final String unitPhone;
  final String unitContact;
  final String unitCorporateName;
  final String lastMovementDate;
  final String cnae;
  final String role;
  final String positionDescription;
  final String unitAddressNumber;
  final String unitAddressComplement;
  final String shiftRegime;
  final String rgIssuer;
  final String riskLevel;
  final String rgIssueDate;
  final String descriptionOrigin;
  final String eSocialCategoryCode;
  final String hrRegistration;
}
