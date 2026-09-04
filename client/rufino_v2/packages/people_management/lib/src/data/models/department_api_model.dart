import '../../domain/entities/department.dart';
import '../../domain/entities/position.dart';
import '../../domain/entities/remuneration.dart';
import '../../domain/entities/role.dart';

/// DTO representing the JSON payload for a department from the API.
class DepartmentApiModel {
  const DepartmentApiModel({
    required this.id,
    required this.name,
    required this.description,
    required this.positions,
  });

  final String id;
  final String name;
  final String description;
  final List<PositionApiModel> positions;

  factory DepartmentApiModel.fromJson(Map<String, dynamic> json) {
    final rawPositions = json['positions'] as List<dynamic>? ?? [];
    return DepartmentApiModel(
      id: json['id'] as String,
      name: json['name'] as String,
      description: json['description'] as String,
      positions: rawPositions
          .map((e) => PositionApiModel.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  factory DepartmentApiModel.fromJsonSimple(Map<String, dynamic> json) {
    return DepartmentApiModel(
      id: json['id'] as String,
      name: json['name'] as String,
      description: json['description'] as String,
      positions: const [],
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'name': name,
        'description': description,
      };

  Map<String, dynamic> toCreateJson() => {
        'name': name,
        'description': description,
      };

  /// Converts this DTO into a domain [Department] entity.
  Department toEntity() => Department(
        id: id,
        name: name,
        description: description,
        positions: positions.map((p) => p.toEntity()).toList(),
      );
}

/// DTO representing the JSON payload for a position from the API.
class PositionApiModel {
  const PositionApiModel({
    required this.id,
    required this.name,
    required this.description,
    required this.cbo,
    required this.roles,
  });

  final String id;
  final String name;
  final String description;
  final String cbo;
  final List<RoleApiModel> roles;

  factory PositionApiModel.fromJson(Map<String, dynamic> json) {
    final rawRoles = json['roles'] as List<dynamic>? ?? [];
    return PositionApiModel(
      id: json['id'] as String,
      name: json['name'] as String,
      description: json['description'] as String,
      cbo: json['cbo'] as String,
      roles: rawRoles
          .map((e) => RoleApiModel.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  factory PositionApiModel.fromJsonSimple(Map<String, dynamic> json) {
    return PositionApiModel(
      id: json['id'] as String,
      name: json['name'] as String,
      description: json['description'] as String,
      cbo: json['cbo'] as String,
      roles: const [],
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'name': name,
        'description': description,
        'cbo': cbo,
      };

  Map<String, dynamic> toCreateJson(String departmentId) => {
        'departmentId': departmentId,
        'name': name,
        'description': description,
        'cbo': cbo,
      };

  /// Converts this DTO into a domain [Position] entity.
  Position toEntity() => Position(
        id: id,
        name: name,
        description: description,
        cbo: cbo,
        roles: roles.map((r) => r.toEntity()).toList(),
      );
}

/// DTO representing the JSON payload for a role from the API.
class RoleApiModel {
  const RoleApiModel({
    required this.id,
    required this.name,
    required this.description,
    required this.cbo,
    required this.remuneration,
  });

  final String id;
  final String name;
  final String description;
  final String cbo;
  final RemunerationApiModel remuneration;

  factory RoleApiModel.fromJson(Map<String, dynamic> json) {
    return RoleApiModel(
      id: json['id'] as String,
      name: json['name'] as String,
      description: json['description'] as String,
      cbo: json['cbo'] as String,
      remuneration: RemunerationApiModel.fromJson(
          json['remuneration'] as Map<String, dynamic>),
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'name': name,
        'description': description,
        'cbo': cbo,
        'remuneration': remuneration.toJson(),
      };

  Map<String, dynamic> toCreateJson(String positionId) => {
        'positionId': positionId,
        'name': name,
        'description': description,
        'cbo': cbo,
        'remuneration': remuneration.toJson(),
      };

  /// Converts this DTO into a domain [Role] entity.
  Role toEntity() => Role(
        id: id,
        name: name,
        description: description,
        cbo: cbo,
        remuneration: remuneration.toEntity(),
      );
}

/// DTO representing the JSON payload for a remuneration block.
class RemunerationApiModel {
  const RemunerationApiModel({
    required this.paymentUnitId,
    required this.paymentUnitName,
    required this.salaryTypeId,
    required this.salaryTypeName,
    required this.baseSalaryValue,
    required this.description,
  });

  final String paymentUnitId;

  /// The payment unit display name (e.g. "Por Mês") as returned by the API.
  final String paymentUnitName;

  final String salaryTypeId;

  /// The salary type / currency display name (e.g. "BRL") as returned by the
  /// API.
  final String salaryTypeName;

  final String baseSalaryValue;
  final String description;

  factory RemunerationApiModel.fromJson(Map<String, dynamic> json) {
    final paymentUnit = json['paymentUnit'] as Map<String, dynamic>;
    final baseSalary = json['baseSalary'] as Map<String, dynamic>;
    final type = baseSalary['type'] as Map<String, dynamic>;
    return RemunerationApiModel(
      paymentUnitId: (paymentUnit['id']).toString(),
      paymentUnitName: paymentUnit['name'] as String? ?? '',
      salaryTypeId: (type['id']).toString(),
      salaryTypeName: type['name'] as String? ?? '',
      baseSalaryValue: (baseSalary['value'] as Object).toString(),
      description: json['description'] as String,
    );
  }

  Map<String, dynamic> toJson() => {
        'paymentUnit': paymentUnitId,
        'baseSalary': {
          'type': salaryTypeId,
          'value': baseSalaryValue,
        },
        'description': description,
      };

  /// Converts this DTO into a domain [Remuneration] entity.
  Remuneration toEntity() => Remuneration(
        paymentUnit: PaymentUnit(id: paymentUnitId, name: paymentUnitName),
        baseSalary: BaseSalary(
          type: SalaryType(id: salaryTypeId, name: salaryTypeName),
          value: baseSalaryValue,
        ),
        description: description,
      );
}

/// DTO representing a single payment unit option from the lookup endpoint.
class PaymentUnitApiModel {
  const PaymentUnitApiModel({required this.id, required this.name});

  final String id;
  final String name;

  factory PaymentUnitApiModel.fromJson(Map<String, dynamic> json) {
    return PaymentUnitApiModel(
      id: (json['id']).toString(),
      name: json['name'] as String,
    );
  }

  /// Converts this DTO into a domain [PaymentUnit] entity.
  PaymentUnit toEntity() => PaymentUnit(id: id, name: name);
}

/// DTO representing a single salary type option from the lookup endpoint.
class SalaryTypeApiModel {
  const SalaryTypeApiModel({required this.id, required this.name});

  final String id;
  final String name;

  factory SalaryTypeApiModel.fromJson(Map<String, dynamic> json) {
    return SalaryTypeApiModel(
      id: (json['id']).toString(),
      name: json['name'] as String,
    );
  }

  /// Converts this DTO into a domain [SalaryType] entity.
  SalaryType toEntity() => SalaryType(id: id, name: name);
}
