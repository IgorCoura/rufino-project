import 'package:equatable/equatable.dart';
import 'package:rufino/modules/employee/domain/model/dependent/dependency_type.dart';
import 'package:rufino/modules/employee/domain/model/gender.dart';
import 'package:rufino/modules/employee/domain/model/id_card/id_card.dart';
import 'package:rufino/modules/employee/domain/model/name.dart';

class Dependent extends Equatable {
  final Name idName;
  final Name name;
  final IdCard idCard;
  final Gender gender;
  final DependencyType type;

  const Dependent(this.idName, this.name, this.idCard, this.gender, this.type);
  const Dependent.empty({
    this.idName = const Name.empty(),
    this.name = const Name.empty(),
    this.idCard = const IdCard.empty(),
    this.gender = const Gender.empty(),
    this.type = const DependencyType.empty(),
  });

  const Dependent.newDepedent({
    this.idName = const Name.empty(),
    this.name = const Name.defaultNameDependent(),
    this.idCard = const IdCard.empty(),
    this.gender = const Gender.empty(),
    this.type = const DependencyType.empty(),
  });

  Dependent copyWith(
      {Name? name,
      IdCard? idCard,
      Gender? gender,
      DependencyType? type,
      Object? generic,
      bool? isLoading,
      bool? isLazyLoading}) {
    if (generic != null) {
      switch (generic.runtimeType) {
        case const (Name):
          name = generic as Name?;
        case const (IdCard):
          idCard = generic as IdCard?;
        case const (Gender):
          gender = generic as Gender?;
        case const (DependencyType):
          type = generic as DependencyType?;
        default:
          idCard = this.idCard.copyWith(generic: generic);
      }
    }
    return Dependent(idName, name ?? this.name, idCard ?? this.idCard,
        gender ?? this.gender, type ?? this.type);
  }

  static Dependent fromJson(Map<String, dynamic> json) {
    return Dependent(
      Name(json["name"]),
      Name(json["name"]),
      IdCard.fromJson(json["idCard"]),
      Gender.fromJson(json["gender"]),
      DependencyType.fromJson(json["dependencyType"]),
    );
  }

  static List<Dependent> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => fromJson(el)).toList();
  }

  Map<String, dynamic> toJsonCreateDependent(String employeeId) {
    return {
      "employeeId": employeeId,
      "name": name.value,
      "idCard": idCard.toJson(),
      "gender": gender.id,
      "dependencyType": type.id,
    };
  }

  Map<String, dynamic> toJsonUpdateDependent(String employeeId) {
    return {
      "employeeId": employeeId,
      "oldName": idName.value,
      "currentDependent": {
        "name": name.value,
        "idCard": idCard.toJson(),
        "gender": gender.id,
        "dependencyType": type.id,
      }
    };
  }

  @override
  List<Object?> get props => [name, idCard, gender, type];
}
