import 'package:equatable/equatable.dart';
import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';
import 'package:rufino/modules/employee/domain/model/personal_info/deficiency.dart';
import 'package:rufino/modules/employee/domain/model/personal_info/education_level.dart';
import 'package:rufino/modules/employee/domain/model/personal_info/ethinicity.dart';
import 'package:rufino/modules/employee/domain/model/personal_info/gender.dart';
import 'package:rufino/modules/employee/domain/model/personal_info/marital_status.dart';

class PersonalInfo extends Equatable {
  final bool isLoading;
  final bool isLazyLoading;
  final Deficiency deficiency;
  final MaritalStatus maritalStatus;
  final Gender gender;
  final Ethinicity ethinicity;
  final EducationLevel educationLevel;

  const PersonalInfo(this.deficiency, this.maritalStatus, this.gender,
      this.ethinicity, this.educationLevel,
      {this.isLoading = false, this.isLazyLoading = false});

  const PersonalInfo.loading(
      {this.deficiency = const Deficiency.empty(),
      this.maritalStatus = const MaritalStatus.empty(),
      this.gender = const Gender.empty(),
      this.ethinicity = const Ethinicity.empty(),
      this.educationLevel = const EducationLevel.empty(),
      this.isLoading = true,
      this.isLazyLoading = false});

  PersonalInfo setdeficiency(Deficiency newDeficiency) => PersonalInfo(
      newDeficiency, maritalStatus, gender, ethinicity, educationLevel);

  PersonalInfo copyWith(
      {Deficiency? deficiency,
      MaritalStatus? maritalStatus,
      Gender? gender,
      Ethinicity? ethinicity,
      EducationLevel? educationLevel,
      Object? generic,
      bool? isLoading,
      bool? isLazyLoading}) {
    if (generic != null) {
      switch (generic.runtimeType) {
        case const (MaritalStatus):
          maritalStatus = generic as MaritalStatus?;
        case const (Gender):
          gender = generic as Gender?;
        case const (Ethinicity):
          ethinicity = generic as Ethinicity?;
        case const (EducationLevel):
          educationLevel = generic as EducationLevel?;
        case const (Deficiency):
          deficiency = generic as Deficiency?;
        default:
          deficiency = this.deficiency.copyWith(generic: generic);
      }
    }
    return PersonalInfo(
      deficiency ?? this.deficiency,
      maritalStatus ?? this.maritalStatus,
      gender ?? this.gender,
      ethinicity ?? this.ethinicity,
      educationLevel ?? this.educationLevel,
      isLoading: isLoading ?? this.isLoading,
      isLazyLoading: isLazyLoading ?? this.isLazyLoading,
    );
  }

  static PersonalInfo fromJson(Map<String, dynamic> json) {
    return PersonalInfo(
        Deficiency.fromJson(json["deficiency"]),
        MaritalStatus.fromJson(json["maritalStatus"]),
        Gender.fromJson(json["gender"]),
        Ethinicity.fromJson(json["ethinicity"]),
        EducationLevel.fromJson(json["educationLevel"]),
        isLoading: false);
  }

  Map<String, dynamic> toJson(String employeeId) {
    return {
      "employeeId": employeeId,
      "deficiency": deficiency.toJson(),
      "maritalStatus": maritalStatus.id,
      "gender": gender.id,
      "ethinicity": ethinicity.id,
      "educationLevel": educationLevel.id,
    };
  }

  List<Enumeration> get propsEnumeration =>
      [maritalStatus, gender, ethinicity, educationLevel];

  @override
  List<Object?> get props => [
        isLoading,
        deficiency,
        maritalStatus,
        gender,
        ethinicity,
        educationLevel
      ];
}
