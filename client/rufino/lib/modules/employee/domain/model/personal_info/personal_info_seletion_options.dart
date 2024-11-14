import 'package:equatable/equatable.dart';
import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';
import 'package:rufino/modules/employee/domain/model/personal_info/disability.dart';
import 'package:rufino/modules/employee/domain/model/personal_info/education_level.dart';
import 'package:rufino/modules/employee/domain/model/personal_info/ethinicity.dart';
import 'package:rufino/modules/employee/domain/model/gender.dart';
import 'package:rufino/modules/employee/domain/model/personal_info/marital_status.dart';

class PersonalInfoSeletionOptions extends Equatable {
  final List<MaritalStatus> maritalStatusOptions;
  final List<Gender> genderOptions;
  final List<Ethinicity> ethinicityOptions;
  final List<EducationLevel> educationLevelOtions;
  final List<Disability> disabilityOptions;

  const PersonalInfoSeletionOptions(
      {this.maritalStatusOptions = const [MaritalStatus.empty()],
      this.genderOptions = const [Gender.empty()],
      this.ethinicityOptions = const [Ethinicity.empty()],
      this.educationLevelOtions = const [EducationLevel.empty()],
      this.disabilityOptions = const [Disability.empty()]});

  static PersonalInfoSeletionOptions fromJson(Map<String, dynamic> json) {
    return PersonalInfoSeletionOptions(
        maritalStatusOptions: MaritalStatus.fromListJson(json["maritalStatus"]),
        genderOptions: Gender.fromListJson(json["gender"]),
        ethinicityOptions: Ethinicity.fromListJson(json["ethinicity"]),
        educationLevelOtions:
            EducationLevel.fromListJson(json["educationLevel"]),
        disabilityOptions: Disability.fromListJson(json["disability"]));
  }

  List<Enumeration> getSelectionOptions(Type type) {
    switch (type) {
      case const (MaritalStatus):
        return maritalStatusOptions;
      case const (Gender):
        return genderOptions;
      case const (Ethinicity):
        return ethinicityOptions;
      case const (EducationLevel):
        return educationLevelOtions;
      case const (Disability):
        return disabilityOptions;
    }
    return [];
  }

  @override
  List<Object?> get props => [
        maritalStatusOptions.hashCode,
        genderOptions.hashCode,
        ethinicityOptions.hashCode,
        educationLevelOtions.hashCode,
        disabilityOptions.hashCode
      ];
}
