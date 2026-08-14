/// Reexporta a família que agora vive em `rufino_core`.
///
/// A consulta de CEP é ViaCEP puro — não cita produto nenhum —, e o cadastro
/// de tenant precisa dela tanto quanto o de funcionário.
///
/// Código novo deve importar `package:rufino_core/rufino_core.dart`.
library;
export 'package:rufino_core/rufino_core.dart'
    show CepException, CepLookupException, CepNotFoundException;
