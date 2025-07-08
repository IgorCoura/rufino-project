import 'package:rufino/shared/errors/aplication_errors.dart';

class CompanyErrors {
  CompanyErrors();
  static const codePrefix = "COMP";
  final _factory = AplicationErrorFactory(codePrefix);

  AplicationException get selectedCompanyErro =>
      _factory.newError(1, "É necessario selecionar um empresa.");
}
