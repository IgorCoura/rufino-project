import 'package:rufino/shared/errors/aplication_errors.dart';

class EmployeesErrors {
  EmployeesErrors();
  final _factory = AplicationErrorFactory("EMP");

  AplicationException get errorTryCreateEmployee => _factory.newError(1,
      "Houve um erro ao tentar criar um novo empregado. Tente novamente mais tarde.");
}
