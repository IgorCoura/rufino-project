import 'dart:typed_data';

import '../../core/result.dart';
import '../entities/address.dart';
import '../entities/employee.dart';
import '../entities/employee_contact.dart';
import '../entities/employee_id_card.dart';
import '../entities/employee_personal_info.dart';
import '../entities/employee_profile.dart';
import '../entities/employee_vote_id.dart';
import '../entities/personal_info_options.dart';

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
}
