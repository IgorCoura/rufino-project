import 'package:equatable/equatable.dart';
import 'package:rufino/modules/employee/domain/model/document_group/name.dart';
import 'package:rufino/modules/employee/domain/model/document_group/description.dart';

class DocumentGroup extends Equatable {
  final String id;
  final Name name;
  final Description description;

  const DocumentGroup(this.id, this.name, this.description);

  const DocumentGroup.empty()
      : id = "",
        name = const Name.empty(),
        description = const Description.empty();

  factory DocumentGroup.fromJson(Map<String, dynamic> json) {
    return DocumentGroup(
      json['id'] as String,
      Name(json['name']),
      Description(json['description']),
    );
  }

  static List<DocumentGroup> fromJsonList(List<dynamic> jsonList) {
    return jsonList
        .map((json) => DocumentGroup.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name.value,
      'description': description.value,
    };
  }

  DocumentGroup copyWith({
    String? id,
    Name? name,
    Description? description,
    Object? generic,
  }) {
    switch (generic.runtimeType) {
      case const (Name):
        name = generic as Name?;
        break;
      case const (Description):
        description = generic as Description?;
        break;
    }

    return DocumentGroup(
        id ?? this.id, name ?? this.name, description ?? this.description);
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
  List<Object?> get props => [id, name, description];
}
