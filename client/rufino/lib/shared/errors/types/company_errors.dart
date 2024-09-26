import 'package:rufino/shared/errors/aplication_errors.dart';

class CompanyErrors {
  CompanyErrors();
  final _factory = AplicationErrorFactory("COMP");

  AplicationException get selectedCompanyErro =>
      _factory.newError(1, "É necessario selecionar um empresa.");
}
