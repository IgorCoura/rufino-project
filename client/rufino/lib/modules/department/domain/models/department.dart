import 'package:equatable/equatable.dart';
import 'package:rufino/modules/department/domain/models/description.dart';
import 'package:rufino/modules/department/domain/models/name.dart';
import 'package:rufino/modules/department/domain/models/position.dart';

class Department extends Equatable {
  final String id;
  final Name name;
  final Description description;
  final List<Position> positions;

  const Department(this.id, this.name, this.description, this.positions);

  const Department.empty()
      : id = "",
        name = const Name.empty(),
        description = const Description.empty(),
        positions = const [];

  factory Department.fromJson(Map<String, dynamic> json) {
    return Department(
      json['id'] as String,
      Name(json['name']),
      Description(json['description']),
      Position.fromJsonList(json['positions']),
    );
  }

  factory Department.fromJsonSimple(Map<String, dynamic> json) {
    return Department(
      json['id'] as String,
      Name(json['name']),
      Description(json['description']),
      [],
    );
  }

  static List<Department> fromJsonList(List<dynamic> jsonList) {
    return jsonList
        .map((json) => Department.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name.value,
      'description': description.value,
    };
  }

  Map<String, dynamic> toJsonCreate() {
    return {
      'name': name.value,
      'description': description.value,
    };
  }

  Department copyWith({
    String? id,
    Name? name,
    Description? description,
    List<Position>? positions,
    Object? generic,
  }) {
    switch (generic.runtimeType) {
      case const (Name):
        name = generic as Name?;
        break;
      case const (Description):
        description = generic as Description?;
        break;
      case const (List<Position>):
        positions = generic as List<Position>?;
        break;
    }

    return Department(
      id ?? this.id,
      name ?? this.name,
      description ?? this.description,
      positions ?? this.positions,
    );
  }

  static String? validateName(String? value) {
    if (value == null || value.isEmpty) {
      return "Não pode ser vazio.";
    }
    if (value.length > 100) {
      return "Não pode ser maior que 100 caracteres.";
    }
    return null;
  }

  @override
  List<Object?> get props => [id, name, description, positions.hashCode];
}
