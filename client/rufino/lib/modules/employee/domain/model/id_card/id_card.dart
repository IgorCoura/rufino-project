import 'package:rufino/modules/employee/domain/model/base/model_base.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';
import 'package:rufino/modules/employee/domain/model/id_card/birthcity.dart';
import 'package:rufino/modules/employee/domain/model/id_card/birthstate.dart';
import 'package:rufino/modules/employee/domain/model/id_card/cpf.dart';
import 'package:rufino/modules/employee/domain/model/id_card/date_of_birth.dart';
import 'package:rufino/modules/employee/domain/model/id_card/father_name.dart';
import 'package:rufino/modules/employee/domain/model/id_card/mother_name.dart';
import 'package:rufino/modules/employee/domain/model/id_card/nacionality.dart';

class IdCard extends ModelBase {
  final Cpf cpf;
  final MotherName motherName;
  final FatherName fatherName;
  final DateOfBirth dateOfBirth;
  final Birthcity birthcity;
  final Birthstate birthstate;
  final Nacionality nacionality;

  const IdCard(this.cpf, this.motherName, this.fatherName, this.dateOfBirth,
      this.birthcity, this.birthstate, this.nacionality,
      {super.isLoading = false, super.isLazyLoading = false});

  const IdCard.empty(
      {this.cpf = const Cpf.empty(),
      this.motherName = const MotherName.empty(),
      this.fatherName = const FatherName.empty(),
      this.dateOfBirth = const DateOfBirth.empty(),
      this.birthcity = const Birthcity.empty(),
      this.birthstate = const Birthstate.empty(),
      this.nacionality = const Nacionality.empty(),
      super.isLoading = false,
      super.isLazyLoading = false});

  const IdCard.loading(
      {this.cpf = const Cpf.empty(),
      this.motherName = const MotherName.empty(),
      this.fatherName = const FatherName.empty(),
      this.dateOfBirth = const DateOfBirth.empty(),
      this.birthcity = const Birthcity.empty(),
      this.birthstate = const Birthstate.empty(),
      this.nacionality = const Nacionality.empty(),
      super.isLoading = true,
      super.isLazyLoading = false});

  IdCard copyWith(
      {Cpf? cpf,
      MotherName? motherName,
      FatherName? fatherName,
      DateOfBirth? dateOfBirth,
      Birthcity? birthcity,
      Birthstate? birthstate,
      Nacionality? nacionality,
      Object? generic,
      bool? isLoading,
      bool? isLazyLoading}) {
    switch (generic.runtimeType) {
      case const (Cpf):
        cpf = generic as Cpf?;
      case const (MotherName):
        motherName = generic as MotherName?;
      case const (FatherName):
        fatherName = generic as FatherName?;
      case const (DateOfBirth):
        dateOfBirth = generic as DateOfBirth?;
      case const (Birthcity):
        birthcity = generic as Birthcity?;
      case const (Birthstate):
        birthstate = generic as Birthstate?;
      case const (Nacionality):
        nacionality = generic as Nacionality?;
    }

    return IdCard(
      cpf ?? this.cpf,
      motherName ?? this.motherName,
      fatherName ?? this.fatherName,
      dateOfBirth ?? this.dateOfBirth,
      birthcity ?? this.birthcity,
      birthstate ?? this.birthstate,
      nacionality ?? this.nacionality,
      isLoading: isLoading ?? this.isLoading,
      isLazyLoading: isLazyLoading ?? this.isLazyLoading,
    );
  }

  factory IdCard.fromJson(Map<String, dynamic> json) {
    return IdCard(
        Cpf.createFormatted(json["cpf"]),
        MotherName(json["motherName"]),
        FatherName(json["fatherName"]),
        json["dateOfBirth"] == null
            ? const DateOfBirth.empty()
            : DateOfBirth.createFormatted(json["dateOfBirth"]),
        Birthcity(json["birthCity"]),
        Birthstate(json["birthState"]),
        Nacionality(json["nacionality"]));
  }

  Map<String, dynamic> toJson({String? employeeId}) {
    return employeeId == null
        ? {
            "cpf": cpf.value,
            "motherName": motherName.value,
            "fatherName": fatherName.value,
            "dateOfBirth": dateOfBirth.toData(),
            "birthCity": birthcity.value,
            "birthState": birthstate.value,
            "nacionality": nacionality.value,
          }
        : {
            "employeeId": employeeId,
            "cpf": cpf.value,
            "motherName": motherName.value,
            "fatherName": fatherName.value,
            "dateOfBirth": dateOfBirth.toData(),
            "birthCity": birthcity.value,
            "birthState": birthstate.value,
            "nacionality": nacionality.value,
          };
  }

  @override
  List<Object?> get props => [
        cpf,
        motherName,
        fatherName,
        dateOfBirth,
        birthcity,
        birthstate,
        nacionality,
        isLoading,
        isLazyLoading
      ];

  List<TextPropBase> get textProps => [
        cpf,
        motherName,
        fatherName,
        dateOfBirth,
        birthcity,
        birthstate,
        nacionality
      ];
}
