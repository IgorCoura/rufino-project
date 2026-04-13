import 'dart:typed_data';

import '../../core/result.dart';
import '../../data/models/document_range_item.dart';
import '../../data/services/multipart_upload_helper.dart';
import '../entities/address.dart';
import '../entities/employee.dart';
import '../entities/employee_contact.dart';
import '../entities/employee_document.dart';
import '../entities/employee_contract.dart';
import '../entities/employee_dependent.dart';
import '../entities/employee_id_card.dart';
import '../entities/employee_medical_exam.dart';
import '../entities/employee_military_document.dart';
import '../entities/employee_personal_info.dart';
import '../entities/employee_profile.dart';
import '../entities/employee_vote_id.dart';
import '../entities/personal_info_options.dart';
import '../entities/selection_option.dart';

/// Contract for accessing and creating employee data.
abstract class EmployeeRepository {
  /// Returns the detailed employee profile identified by [employeeId].
  Future<Result<EmployeeProfile>> getEmployeeProfile(
    String companyId,
    String employeeId,
  );

  /// Returns a page of employees for [companyId] matching the given filters.
  ///
  /// [name] filters by employee name (null = no filter).
  /// [role] filters by role name (null = no filter).
  /// [status] filters by [EmployeeStatus] (null = all statuses).
  /// [documentStatus] filters by [DocumentStatus] (null = all statuses).
  /// [ascending] when true results are sorted A→Z; false for Z→A.
  /// [pageSize] is the maximum number of results to return.
  /// [sizeSkip] is the number of records to skip (offset-based pagination).
  Future<Result<List<Employee>>> getEmployees(
    String companyId, {
    String? name,
    String? role,
    int? status,
    int? documentStatus,
    bool ascending = true,
    int pageSize = 15,
    int sizeSkip = 0,
  });

  /// Returns the profile image bytes for [employeeId], or null if none exists.
  Future<Result<Uint8List?>> getEmployeeImage(
      String companyId, String employeeId);

  /// Uploads a new profile photo for [employeeId].
  ///
  /// [imageBytes] is the raw image data. [fileName] should include the file
  /// extension (e.g. `"photo.jpg"`).
  Future<Result<void>> uploadEmployeeImage(
    String companyId,
    String employeeId,
    Uint8List imageBytes,
    String fileName,
  );

  /// Updates the full name of the employee identified by [employeeId].
  Future<Result<void>> editEmployeeName(
    String companyId,
    String employeeId,
    String name,
  );

  /// Marks the employee identified by [employeeId] as inactive.
  Future<Result<void>> markEmployeeAsInactive(
    String companyId,
    String employeeId,
  );

  /// Creates a new employee and returns the generated id.
  ///
  /// [companyId] is the owning company.
  /// [name] is the full name of the employee.
  /// [roleId] is the id of the assigned role.
  /// [workplaceId] is the id of the assigned workplace.
  Future<Result<String>> createEmployee(
    String companyId, {
    required String name,
    required String roleId,
    required String workplaceId,
  });

  /// Returns the contact information for the employee identified by [employeeId].
  Future<Result<EmployeeContact>> getEmployeeContact(
    String companyId,
    String employeeId,
  );

  /// Updates the contact information for the employee identified by [employeeId].
  Future<Result<void>> editEmployeeContact(
    String companyId,
    String employeeId,
    String cellphone,
    String email,
  );

  /// Returns the address for the employee identified by [employeeId].
  Future<Result<Address>> getEmployeeAddress(
    String companyId,
    String employeeId,
  );

  /// Updates the address for the employee identified by [employeeId].
  Future<Result<void>> editEmployeeAddress(
    String companyId,
    String employeeId,
    Address address,
  );

  /// Returns the personal info for the employee identified by [employeeId].
  Future<Result<EmployeePersonalInfo>> getEmployeePersonalInfo(
    String companyId,
    String employeeId,
  );

  /// Returns the available selection options for the personal info form.
  Future<Result<PersonalInfoOptions>> getPersonalInfoOptions(String companyId);

  /// Updates the personal info for the employee identified by [employeeId].
  Future<Result<void>> editEmployeePersonalInfo(
    String companyId,
    String employeeId,
    EmployeePersonalInfo personalInfo,
  );

  /// Returns the ID card (Identidade) information for the employee identified
  /// by [employeeId].
  Future<Result<EmployeeIdCard>> getEmployeeIdCard(
    String companyId,
    String employeeId,
  );

  /// Updates the ID card (Identidade) information for the employee identified
  /// by [employeeId].
  Future<Result<void>> editEmployeeIdCard(
    String companyId,
    String employeeId,
    EmployeeIdCard idCard,
  );

  /// Returns the voter registration (Título de Eleitor) for the employee
  /// identified by [employeeId].
  Future<Result<EmployeeVoteId>> getEmployeeVoteId(
    String companyId,
    String employeeId,
  );

  /// Updates the voter registration (Título de Eleitor) for the employee
  /// identified by [employeeId].
  Future<Result<void>> editEmployeeVoteId(
    String companyId,
    String employeeId,
    String voteIdNumber,
  );

  /// Returns the military document (Documento Militar) for the employee
  /// identified by [employeeId].
  Future<Result<EmployeeMilitaryDocument>> getMilitaryDocument(
    String companyId,
    String employeeId,
  );

  /// Updates the military document (Documento Militar) for the employee
  /// identified by [employeeId].
  Future<Result<void>> editMilitaryDocument(
    String companyId,
    String employeeId,
    String number,
    String type,
  );

  /// Returns the medical admission exam (Exame Médico Admissional) for the
  /// employee identified by [employeeId].
  Future<Result<EmployeeMedicalExam>> getMedicalExam(
    String companyId,
    String employeeId,
  );

  /// Updates the medical admission exam for the employee identified by
  /// [employeeId].
  ///
  /// [dateExam] and [validityExam] must be in `dd/MM/yyyy` display format.
  Future<Result<void>> editMedicalExam(
    String companyId,
    String employeeId,
    String dateExam,
    String validityExam,
  );

  /// Updates the role assignment for the employee identified by [employeeId].
  Future<Result<void>> editEmployeeRole(
    String companyId,
    String employeeId,
    String roleId,
  );

  /// Returns the list of dependents for the employee identified by [employeeId].
  Future<Result<List<EmployeeDependent>>> getDependents(
    String companyId,
    String employeeId,
  );

  /// Creates a new dependent for the employee identified by [employeeId].
  Future<Result<void>> createDependent(
    String companyId,
    String employeeId,
    EmployeeDependent dependent,
  );

  /// Updates an existing dependent for the employee identified by [employeeId].
  Future<Result<void>> editDependent(
    String companyId,
    String employeeId,
    EmployeeDependent dependent,
  );

  /// Removes a dependent by [dependentName] from the employee identified by
  /// [employeeId].
  Future<Result<void>> removeDependent(
    String companyId,
    String employeeId,
    String dependentName,
  );

  /// Updates the workplace assignment for the employee identified by
  /// [employeeId].
  Future<Result<void>> editEmployeeWorkplace(
    String companyId,
    String employeeId,
    String workplaceId,
  );

  /// Returns the list of contracts for the employee identified by [employeeId].
  Future<Result<List<EmployeeContractInfo>>> getContracts(
    String companyId,
    String employeeId,
  );

  /// Returns the available contract type options.
  Future<Result<List<SelectionOption>>> getContractTypes(String companyId);

  /// Creates a new contract (admission) for the employee.
  ///
  /// [initDate] must be in `dd/MM/yyyy` display format.
  Future<Result<void>> createContract(
    String companyId,
    String employeeId,
    String initDate,
    String contractTypeId,
    String registration,
  );

  /// Finishes (ends) the active contract for the employee.
  ///
  /// [finalDate] must be in `dd/MM/yyyy` display format.
  Future<Result<void>> finishContract(
    String companyId,
    String employeeId,
    String finalDate,
  );

  /// Returns the available document signing options.
  Future<Result<List<SelectionOption>>> getDocumentSigningOptions(
      String companyId);

  /// Updates the document signing option for the employee identified by
  /// [employeeId].
  Future<Result<void>> editDocumentSigningOptions(
    String companyId,
    String employeeId,
    String optionId,
  );

  /// Returns the list of required documents (without units) for the employee.
  Future<Result<List<EmployeeDocument>>> getDocuments(
    String companyId,
    String employeeId,
  );

  /// Returns a single document with paginated units.
  Future<Result<EmployeeDocument>> getDocumentById(
    String companyId,
    String employeeId,
    String documentId, {
    int pageNumber = 1,
    int pageSize = 10,
    int? statusId,
  });

  /// Creates a new document unit for the given [documentId].
  Future<Result<void>> createDocumentUnit(
    String companyId,
    String employeeId,
    String documentId,
  );

  /// Updates the date of an existing document unit.
  ///
  /// [date] must be in `dd/MM/yyyy` display format.
  Future<Result<void>> editDocumentUnit(
    String companyId,
    String employeeId,
    String documentId,
    String documentUnitId,
    String date,
  );

  /// Marks a document unit as not applicable.
  Future<Result<void>> setDocumentUnitNotApplicable(
    String companyId,
    String employeeId,
    String documentId,
    String documentUnitId,
  );

  /// Generates a PDF for a document unit and returns the raw bytes.
  Future<Result<Uint8List>> generateDocument(
    String companyId,
    String employeeId,
    String documentId,
    String documentUnitId,
  );

  /// Generates a document and sends it for digital signature.
  Future<Result<void>> generateAndSendToSign(
    String companyId,
    String employeeId,
    String documentId,
    String documentUnitId,
    String dateLimitToSign,
    int reminderEveryNDays,
  );

  /// Downloads the file attached to a document unit.
  Future<Result<Uint8List>> downloadDocumentUnit(
    String companyId,
    String employeeId,
    String documentId,
    String documentUnitId,
  );

  /// Uploads a file to a document unit.
  ///
  /// Reports upload progress through [onProgress] with values from 0.0 to 1.0.
  Future<Result<void>> uploadDocumentUnit(
    String companyId,
    String employeeId,
    String documentId,
    String documentUnitId,
    Uint8List fileBytes,
    String fileName, {
    UploadProgressCallback? onProgress,
  });

  /// Uploads a file to a document unit and sends it for digital signature.
  ///
  /// Reports upload progress through [onProgress] with values from 0.0 to 1.0.
  Future<Result<void>> uploadDocumentUnitToSign(
    String companyId,
    String employeeId,
    String documentId,
    String documentUnitId,
    Uint8List fileBytes,
    String fileName,
    String dateLimitToSign,
    int reminderEveryNDays, {
    UploadProgressCallback? onProgress,
  });

  /// Generates PDFs for multiple document units and returns the ZIP bytes.
  Future<Result<Uint8List>> generateDocumentRange(
    String companyId,
    String employeeId,
    List<DocumentRangeItem> items,
  );

  /// Downloads files for multiple document units and returns the ZIP bytes.
  Future<Result<Uint8List>> downloadDocumentRange(
    String companyId,
    String employeeId,
    List<DocumentRangeItem> items,
  );
}
