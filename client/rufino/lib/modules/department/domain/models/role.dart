import 'package:equatable/equatable.dart';
import 'package:rufino/modules/department/domain/models/CBO.dart';
import 'package:rufino/modules/department/domain/models/description.dart';
import 'package:rufino/modules/department/domain/models/name.dart';
import 'package:rufino/modules/department/domain/models/remuneration.dart';

class Role extends Equatable {
  final String id;
  final Name name;
  final Description description;
  final CBO cbo;
  final Remuneration remuneration;

  const Role(this.id, this.name, this.description, this.cbo, this.remuneration);

  const Role.empty()
      : id = "",
        name = const Name.empty(),
        description = const Description.empty(),
        cbo = const CBO.empty(),
        remuneration = const Remuneration.empty();

  factory Role.fromJson(Map<String, dynamic> json) {
    return Role(
      json['id'] as String,
      Name(json['name']),
      Description(json['description']),
      CBO(json['cbo']),
      Remuneration.fromJson(json['remuneration'] as Map<String, dynamic>),
    );
  }

  static List<Role> fromJsonList(List<dynamic> jsonList) {
    return jsonList
        .map((json) => Role.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name.value,
      'description': description.value,
      'cbo': cbo.value,
      'remuneration': remuneration.toJson(),
    };
  }

  Map<String, dynamic> toJsonCreate() {
    return {
      'name': name.value,
      'description': description.value,
      'cbo': cbo.value,
      'remuneration': remuneration.toJson(),
    };
  }

  Role copyWith({
    Name? name,
    Description? description,
    CBO? cbo,
    Remuneration? remuneration,
    Object? generic,
  }) {
    switch (generic.runtimeType) {
      case const (Name):
        name = generic as Name?;
        break;
      case const (Description):
        description = generic as Description?;
        break;
      case const (CBO):
        cbo = generic as CBO?;
        break;
      case const (Remuneration):
        remuneration = generic as Remuneration?;
        break;
      default:
        remuneration = this.remuneration.copyWith(generic: generic);
        break;
    }

    return Role(
      id,
      name ?? this.name,
      description ?? this.description,
      cbo ?? this.cbo,
      remuneration ?? this.remuneration,
    );
  }

  @override
  List<Object?> get props => [id, name, description, cbo, remuneration];
}
