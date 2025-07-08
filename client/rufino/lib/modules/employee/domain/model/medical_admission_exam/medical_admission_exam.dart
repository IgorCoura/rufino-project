import 'package:rufino/modules/employee/domain/model/base/model_base.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';
import 'package:rufino/modules/employee/domain/model/medical_admission_exam/date_exam.dart';
import 'package:rufino/modules/employee/domain/model/medical_admission_exam/validity.dart';

class MedicalAdmissionExam extends ModelBase {
  final DateExam dateExam;
  final Validity validity;

  const MedicalAdmissionExam(this.dateExam, this.validity,
      {super.isLoading = false, super.isLazyLoading = false});

  const MedicalAdmissionExam.empty(
      {this.dateExam = const DateExam.empty(),
      this.validity = const Validity.empty(),
      super.isLoading = false,
      super.isLazyLoading = false});

  const MedicalAdmissionExam.loading(
      {this.dateExam = const DateExam.empty(),
      this.validity = const Validity.empty(),
      super.isLoading = true,
      super.isLazyLoading = false});

  MedicalAdmissionExam copyWith({
    DateExam? dateExam,
    Validity? validity,
    Object? generic,
    bool? isLoading,
    bool? isLazyLoading,
  }) {
    switch (generic.runtimeType) {
      case const (DateExam):
        dateExam = generic as DateExam?;
      case const (Validity):
        validity = generic as Validity?;
    }

    return MedicalAdmissionExam(
      dateExam ?? this.dateExam,
      validity ?? this.validity,
      isLoading: isLoading ?? this.isLoading,
      isLazyLoading: isLazyLoading ?? this.isLazyLoading,
    );
  }

  factory MedicalAdmissionExam.fromJson(Map<String, dynamic> json) {
    return MedicalAdmissionExam(
      DateExam.createFormatted(json['dateExam']),
      Validity.createFormatted(json['validityExam']),
    );
  }

  Map<String, dynamic> toJson(String employeeId) {
    return {
      "employeeId": employeeId,
      "dateExam": dateExam.toData(),
      "validityExam": validity.toData(),
    };
  }

  @override
  List<Object?> get props => [dateExam, validity, isLoading, isLazyLoading];

  @override
  List<TextPropBase> get textProps => [dateExam, validity];
}
