import 'package:rufino/modules/employee/domain/model/base/model_base.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';
import 'package:rufino/modules/employee/domain/model/military_document/number_military_document.dart';
import 'package:rufino/modules/employee/domain/model/military_document/typer_military_document.dart';

class MilitaryDocument extends ModelBase {
  final NumberMilitaryDocument number;
  final TyperMilitaryDocument type;
  final bool isRequired;

  const MilitaryDocument(this.number, this.type, this.isRequired,
      {super.isLoading, super.isLazyLoading});

  const MilitaryDocument.empty(
      {this.number = const NumberMilitaryDocument.empty(),
      this.isRequired = false,
      this.type = const TyperMilitaryDocument.empty(),
      super.isLoading,
      super.isLazyLoading});

  const MilitaryDocument.loading(
      {this.number = const NumberMilitaryDocument.empty(),
      this.isRequired = false,
      this.type = const TyperMilitaryDocument.empty(),
      super.isLoading = true,
      super.isLazyLoading});

  MilitaryDocument copyWith(
      {NumberMilitaryDocument? number,
      TyperMilitaryDocument? type,
      bool? isRequired,
      Object? generic,
      bool? isLoading,
      bool? isLazyLoading}) {
    switch (generic.runtimeType) {
      case const (NumberMilitaryDocument):
        number = generic as NumberMilitaryDocument?;
      case const (TyperMilitaryDocument):
        type = generic as TyperMilitaryDocument?;
    }
    return MilitaryDocument(
      number ?? this.number,
      type ?? this.type,
      isRequired ?? this.isRequired,
      isLoading: isLoading ?? this.isLoading,
      isLazyLoading: isLazyLoading ?? this.isLazyLoading,
    );
  }

  factory MilitaryDocument.fromJson(Map<String, dynamic> json) {
    return MilitaryDocument(
      NumberMilitaryDocument(json["number"]),
      TyperMilitaryDocument(json["type"]),
      json["isRequired"],
    );
  }
  Map<String, dynamic> toJson(String employeeId) {
    return {
      "employeeId": employeeId,
      "number": number.value,
      "type": type.value,
    };
  }

  @override
  List<Object?> get props => [number, type, isLoading, isLazyLoading];

  List<TextPropBase> get textProps => [number, type];
}
