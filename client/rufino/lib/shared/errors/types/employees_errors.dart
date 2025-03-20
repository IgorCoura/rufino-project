import 'package:rufino/shared/errors/aplication_errors.dart';

class EmployeesErrors {
  EmployeesErrors();
  static const codePrefix = "EMP";
  final _factory = AplicationErrorFactory(codePrefix);

  AplicationException get errorTryCreateEmployee => _factory.newError(1,
      "Houve um erro ao tentar criar um novo empregado. Tente novamente mais tarde.");
}
