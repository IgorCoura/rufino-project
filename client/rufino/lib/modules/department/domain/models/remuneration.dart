import 'package:equatable/equatable.dart';
import 'package:rufino/modules/department/domain/models/description.dart';

class Remuneration extends Equatable {
  final PaymentUnit paymentUnit;
  final BaseSalary baseSalary;
  final SecundaryDescription description;

  const Remuneration(this.paymentUnit, this.baseSalary, this.description);

  // Empty constructor
  const Remuneration.empty()
      : paymentUnit = const PaymentUnit.empty(),
        baseSalary = const BaseSalary.empty(),
        description = const SecundaryDescription.empty();

  // fromJson constructor
  factory Remuneration.fromJson(Map<String, dynamic> json) {
    return Remuneration(
      PaymentUnit.fromJson(json['paymentUnit']),
      BaseSalary.fromJson(json['baseSalary']),
      SecundaryDescription(json['description']),
    );
  }

  // fromJsonList constructor
  static List<Remuneration> fromJsonList(List<dynamic> jsonList) {
    return jsonList.map((json) => Remuneration.fromJson(json)).toList();
  }

  // copyWith method
  Remuneration copyWith({
    PaymentUnit? paymentUnit,
    BaseSalary? baseSalary,
    SecundaryDescription? description,
    Object? generic,
  }) {
    switch (generic.runtimeType) {
      case const (PaymentUnit):
        paymentUnit = generic as PaymentUnit?;
        break;
      case const (BaseSalary):
        baseSalary = generic as BaseSalary?;
        break;
      case const (SecundaryDescription):
        description = generic as SecundaryDescription?;
        break;
      default:
        baseSalary = this.baseSalary.copyWith(
              generic: generic,
            );
        break;
    }
    return Remuneration(
      paymentUnit ?? this.paymentUnit,
      baseSalary ?? this.baseSalary,
      description ?? this.description,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'paymentUnit': paymentUnit.id,
      'baseSalary': baseSalary.toJson(),
      'description': description.value,
    };
  }

  @override
  List<Object?> get props => [paymentUnit, baseSalary, description];
}

class PaymentUnit extends Equatable {
  static const Map<String, String> conversionMapIntToString = {
    "0": "Não Aplicável",
    "1": "Por Hora",
    "2": "Por Dia",
    "3": "Por Semana",
    "4": "Por Quinzena",
    "5": "Por Mês",
    "6": "Por Tarefa",
  };

  final String id;
  final String _name;

  String get name => conversionMapIntToString[id] ?? _name;

  const PaymentUnit(this.id, this._name);

  const PaymentUnit.empty()
      : id = "",
        _name = '';

  factory PaymentUnit.fromJson(Map<String, dynamic> json) {
    return PaymentUnit(
      (json['id']).toString(),
      json['name'] as String,
    );
  }

  static List<PaymentUnit> fromJsonList(List<dynamic> jsonList) {
    return jsonList.map((json) => PaymentUnit.fromJson(json)).toList();
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
    };
  }

  @override
  List<Object?> get props => [id, name];
}

class BaseSalary extends Equatable {
  final SalaryType type;
  final MonetaryValue value;

  const BaseSalary(this.type, this.value);

  const BaseSalary.empty()
      : type = const SalaryType.empty(),
        value = const MonetaryValue.empty();

  factory BaseSalary.fromJson(Map<String, dynamic> json) {
    return BaseSalary(
      SalaryType.fromJson(json['type']),
      MonetaryValue(json['value']),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'type': type.id,
      'value': value.value,
    };
  }

  BaseSalary copyWith({
    SalaryType? type,
    MonetaryValue? value,
    Object? generic,
  }) {
    switch (generic.runtimeType) {
      case const (SalaryType):
        type = generic as SalaryType?;
        break;
      case const (MonetaryValue):
        value = generic as MonetaryValue?;
        break;
    }
    return BaseSalary(
      type ?? this.type,
      value ?? this.value,
    );
  }

  @override
  List<Object?> get props => [type, value];
}

class SalaryType extends Equatable {
  static const Map<String, String> conversionMapIntToString = {
    "1": "BRL",
    "2": "USD",
    "3": "EUR",
  };

  final String id;
  final String _name;

  String get name => conversionMapIntToString[id] ?? _name;

  const SalaryType(this.id, this._name);
  const SalaryType.empty()
      : id = "",
        _name = '';

  factory SalaryType.fromJson(Map<String, dynamic> json) {
    return SalaryType(
      (json['id']).toString(),
      json['name'] as String,
    );
  }

  static List<SalaryType> fromJsonList(List<dynamic> jsonList) {
    return jsonList.map((json) => SalaryType.fromJson(json)).toList();
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
    };
  }

  @override
  List<Object?> get props => [id, name];
}

class MonetaryValue extends Equatable {
  final String _value;

  String get value => _value.replaceAll(",", ".");

  const MonetaryValue(this._value);

  const MonetaryValue.empty() : _value = "";

  static String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "Não pode ser vazio.";
    }

    value = value.replaceAll(",", ".");

    final regex = RegExp(r'^\d+(\.\d{1,2})?$');
    if (!regex.hasMatch(value)) {
      return "Valor inválido.";
    }
    return null;
  }

  @override
  List<Object?> get props => [value];
}
