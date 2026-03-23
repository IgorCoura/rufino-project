import 'dart:typed_data';

import '../../core/errors/employee_exception.dart';
import '../../core/result.dart';
import '../../domain/entities/address.dart';
import '../../domain/entities/employee.dart';
import '../../domain/entities/employee_contact.dart';
import '../../domain/entities/employee_dependent.dart';
import '../../domain/entities/employee_id_card.dart';
import '../../domain/entities/employee_personal_info.dart';
import '../../domain/entities/employee_profile.dart';
import '../../domain/entities/employee_medical_exam.dart';
import '../../domain/entities/employee_military_document.dart';
import '../../domain/entities/employee_vote_id.dart';
import '../../domain/entities/personal_info_options.dart';
import '../../domain/repositories/employee_repository.dart';
import '../models/employee_address_api_model.dart';
import '../models/employee_dependent_api_model.dart';
import '../models/employee_id_card_api_model.dart';
import '../models/employee_medical_exam_api_model.dart';
import '../models/employee_military_document_api_model.dart';
import '../services/employee_api_service.dart';

/// Concrete implementation of [EmployeeRepository] backed by [EmployeeApiService].
///
/// All service calls are wrapped in try/catch. [EmployeeException] subtypes
/// are propagated as-is; all other errors are wrapped in [EmployeeNetworkException].
class EmployeeRepositoryImpl implements EmployeeRepository {
  const EmployeeRepositoryImpl({required this.apiService});

  final EmployeeApiService apiService;

  @override
  Future<Result<EmployeeProfile>> getEmployeeProfile(
    String companyId,
    String employeeId,
  ) async {
    try {
      final model = await apiService.getEmployeeProfile(companyId, employeeId);
      return Result.success(model.toEntity());
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<List<Employee>>> getEmployees(
    String companyId, {
    String? name,
    String? role,
    int? status,
    int? documentStatus,
    bool ascending = true,
    int pageSize = 15,
    int sizeSkip = 0,
  }) async {
    try {
      final models = await apiService.getEmployees(
        companyId,
        name: name,
        role: role,
        status: status,
        documentStatus: documentStatus,
        sortOrder: ascending ? 0 : 1,
        pageSize: pageSize,
        sizeSkip: sizeSkip,
      );
      return Result.success(models.map((m) => m.toEntity()).toList());
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<Uint8List?>> getEmployeeImage(
      String companyId, String employeeId) async {
    try {
      final bytes = await apiService.getEmployeeImage(companyId, employeeId);
      return Result.success(bytes);
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<void>> uploadEmployeeImage(
    String companyId,
    String employeeId,
    Uint8List imageBytes,
    String fileName,
  ) async {
    try {
      await apiService.uploadEmployeeImage(
          companyId, employeeId, imageBytes, fileName);
      return const Result<void>.success(null);
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<void>> editEmployeeName(
    String companyId,
    String employeeId,
    String name,
  ) async {
    try {
      await apiService.editEmployeeName(companyId, employeeId, name);
      return const Result<void>.success(null);
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<void>> markEmployeeAsInactive(
    String companyId,
    String employeeId,
  ) async {
    try {
      await apiService.markEmployeeAsInactive(companyId, employeeId);
      return const Result<void>.success(null);
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<String>> createEmployee(
    String companyId, {
    required String name,
    required String roleId,
    required String workplaceId,
  }) async {
    try {
      final id = await apiService.createEmployee(
        companyId,
        name: name,
        roleId: roleId,
        workplaceId: workplaceId,
      );
      return Result.success(id);
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<EmployeeContact>> getEmployeeContact(
    String companyId,
    String employeeId,
  ) async {
    try {
      final model =
          await apiService.getEmployeeContact(companyId, employeeId);
      return Result.success(model.toEntity());
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<void>> editEmployeeContact(
    String companyId,
    String employeeId,
    String cellphone,
    String email,
  ) async {
    try {
      await apiService.editEmployeeContact(
          companyId, employeeId, cellphone, email);
      return const Result<void>.success(null);
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<Address>> getEmployeeAddress(
    String companyId,
    String employeeId,
  ) async {
    try {
      final model =
          await apiService.getEmployeeAddress(companyId, employeeId);
      return Result.success(model.toEntity());
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<void>> editEmployeeAddress(
    String companyId,
    String employeeId,
    Address address,
  ) async {
    try {
      final body = EmployeeAddressApiModel.toJsonMap(address, employeeId);
      await apiService.editEmployeeAddress(companyId, employeeId, body);
      return const Result<void>.success(null);
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<EmployeePersonalInfo>> getEmployeePersonalInfo(
    String companyId,
    String employeeId,
  ) async {
    try {
      final model =
          await apiService.getEmployeePersonalInfo(companyId, employeeId);
      return Result.success(model.toEntity());
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<PersonalInfoOptions>> getPersonalInfoOptions(
    String companyId,
  ) async {
    try {
      final model = await apiService.getPersonalInfoOptions(companyId);
      return Result.success(model.toEntity());
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<void>> editEmployeePersonalInfo(
    String companyId,
    String employeeId,
    EmployeePersonalInfo personalInfo,
  ) async {
    try {
      final body = {
        'employeeId': employeeId,
        'deficiency': {
          'disability': personalInfo.disabilityIds
              .map((id) => int.tryParse(id) ?? id)
              .toList(),
          'observation': personalInfo.disabilityObservation,
        },
        'maritalStatus': int.tryParse(personalInfo.maritalStatusId) ??
            personalInfo.maritalStatusId,
        'gender':
            int.tryParse(personalInfo.genderId) ?? personalInfo.genderId,
        'ethinicity': int.tryParse(personalInfo.ethnicityId) ??
            personalInfo.ethnicityId,
        'educationLevel': int.tryParse(personalInfo.educationLevelId) ??
            personalInfo.educationLevelId,
      };
      await apiService.editEmployeePersonalInfo(companyId, employeeId, body);
      return const Result<void>.success(null);
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<EmployeeIdCard>> getEmployeeIdCard(
    String companyId,
    String employeeId,
  ) async {
    try {
      final model =
          await apiService.getEmployeeIdCard(companyId, employeeId);
      return Result.success(model.toEntity());
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<void>> editEmployeeIdCard(
    String companyId,
    String employeeId,
    EmployeeIdCard idCard,
  ) async {
    try {
      final body = {
        'employeeId': employeeId,
        'cpf': idCard.cpf,
        'motherName': idCard.motherName,
        'fatherName': idCard.fatherName,
        'dateOfBirth': EmployeeIdCardApiModel.dateToApi(idCard.dateOfBirth),
        'birthCity': idCard.birthCity,
        'birthState': idCard.birthState,
        'nacionality': idCard.nationality,
      };
      await apiService.editEmployeeIdCard(companyId, employeeId, body);
      return const Result<void>.success(null);
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<EmployeeVoteId>> getEmployeeVoteId(
    String companyId,
    String employeeId,
  ) async {
    try {
      final model =
          await apiService.getEmployeeVoteId(companyId, employeeId);
      return Result.success(model.toEntity());
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<void>> editEmployeeVoteId(
    String companyId,
    String employeeId,
    String voteIdNumber,
  ) async {
    try {
      await apiService.editEmployeeVoteId(companyId, employeeId, voteIdNumber);
      return const Result<void>.success(null);
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<EmployeeMilitaryDocument>> getMilitaryDocument(
    String companyId,
    String employeeId,
  ) async {
    try {
      final model =
          await apiService.getMilitaryDocument(companyId, employeeId);
      return Result.success(model.toEntity());
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<void>> editMilitaryDocument(
    String companyId,
    String employeeId,
    String number,
    String type,
  ) async {
    try {
      final body = EmployeeMilitaryDocumentApiModel.toJsonMap(
          employeeId, number, type);
      await apiService.editMilitaryDocument(companyId, employeeId, body);
      return const Result<void>.success(null);
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<EmployeeMedicalExam>> getMedicalExam(
    String companyId,
    String employeeId,
  ) async {
    try {
      final model = await apiService.getMedicalExam(companyId, employeeId);
      return Result.success(model.toEntity());
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<void>> editMedicalExam(
    String companyId,
    String employeeId,
    String dateExam,
    String validityExam,
  ) async {
    try {
      final body = EmployeeMedicalExamApiModel.toJsonMap(
          employeeId, dateExam, validityExam);
      await apiService.editMedicalExam(companyId, employeeId, body);
      return const Result<void>.success(null);
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<void>> editEmployeeRole(
    String companyId,
    String employeeId,
    String roleId,
  ) async {
    try {
      await apiService.editEmployeeRole(companyId, employeeId, roleId);
      return const Result<void>.success(null);
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<List<EmployeeDependent>>> getDependents(
    String companyId,
    String employeeId,
  ) async {
    try {
      final models = await apiService.getDependents(companyId, employeeId);
      return Result.success(models.map((m) => m.toEntity()).toList());
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<void>> createDependent(
    String companyId,
    String employeeId,
    EmployeeDependent dependent,
  ) async {
    try {
      final body =
          EmployeeDependentApiModel.toCreateJson(employeeId, dependent);
      await apiService.createDependent(companyId, body);
      return const Result<void>.success(null);
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<void>> editDependent(
    String companyId,
    String employeeId,
    EmployeeDependent dependent,
  ) async {
    try {
      final body =
          EmployeeDependentApiModel.toUpdateJson(employeeId, dependent);
      await apiService.editDependent(companyId, body);
      return const Result<void>.success(null);
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }

  @override
  Future<Result<void>> removeDependent(
    String companyId,
    String employeeId,
    String dependentName,
  ) async {
    try {
      final body =
          EmployeeDependentApiModel.toRemoveJson(employeeId, dependentName);
      await apiService.removeDependent(companyId, body);
      return const Result<void>.success(null);
    } on EmployeeException catch (e) {
      return Result.error(e);
    } catch (e) {
      return Result.error(EmployeeNetworkException(e));
    }
  }
}
