import 'package:equatable/equatable.dart';
import 'package:rufino/modules/department/domain/models/description.dart';
import 'package:rufino/modules/department/domain/models/name.dart';
import 'package:rufino/modules/department/domain/models/role.dart';
import 'package:rufino/modules/department/domain/models/CBO.dart';

class Position extends Equatable {
  final String id;
  final Name name;
  final Description description;
  final CBO cbo;
  final List<Role> roles;

  const Position(this.id, this.name, this.description, this.cbo,
      [this.roles = const []]);

  const Position.empty()
      : id = "",
        name = const Name.empty(),
        description = const Description.empty(),
        cbo = const CBO.empty(),
        roles = const [];

  factory Position.fromJson(Map<String, dynamic> json) {
    return Position(
      json['id'] as String,
      Name(json['name']),
      Description(json['description']),
      CBO(json['cbo']),
      Role.fromJsonList(json['roles']),
    );
  }

  factory Position.fromJsonSimple(Map<String, dynamic> json) {
    return Position(
      json['id'] as String,
      Name(json['name']),
      Description(json['description']),
      CBO(json['cbo']),
    );
  }

  static List<Position> fromJsonList(List<dynamic> jsonList) {
    return jsonList
        .map((json) => Position.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name.value,
      'description': description.value,
      'cbo': cbo.value
    };
  }

  Map<String, dynamic> toJsonCreate() {
    return {
      'name': name.value,
      'description': description.value,
      'cbo': cbo.value,
    };
  }

  Position copyWith({
    String? id,
    Name? name,
    Description? description,
    CBO? cbo,
    List<Role>? roles,
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
      case const (List<Role>):
        roles = generic as List<Role>?;
        break;
    }

    return Position(
      id ?? this.id,
      name ?? this.name,
      description ?? this.description,
      cbo ?? this.cbo,
      roles ?? this.roles,
    );
  }

  @override
  List<Object?> get props => [id, name, description, cbo, roles.hashCode];
}
