import 'dart:convert';
import 'package:intl/intl.dart';
import 'package:rufino/modules/employee/domain/model/archive_category/archive_category.dart';
import 'package:rufino/modules/employee/domain/model/archive_category/event.dart'
    as archive;
import 'package:rufino/modules/employee/domain/model/document_signing_options.dart';
import 'package:rufino/modules/employee/domain/model/require_document/event.dart'
    as require_document;
import 'package:rufino/modules/employee/domain/model/dependent/dependency_type.dart';
import 'package:rufino/modules/employee/domain/model/dependent/dependent.dart';
import 'package:rufino/modules/employee/domain/model/employee_contract.dart';
import 'package:rufino/modules/employee/domain/model/employee_contract_type.dart';
import 'package:rufino/modules/employee/domain/model/gender.dart';
import 'package:rufino/modules/employee/domain/model/medical_admission_exam/medical_admission_exam.dart';
import 'package:rufino/modules/employee/domain/model/military_document/military_document.dart';
import 'package:rufino/modules/employee/domain/model/require_document/association.dart';
import 'package:rufino/modules/employee/domain/model/require_document/association_type.dart';
import 'package:rufino/modules/employee/domain/model/require_document/require_document.dart';
import 'package:rufino/modules/employee/domain/model/require_document/require_document_simple.dart';
import 'package:rufino/modules/employee/domain/model/vote_id/vote_id.dart';
import 'package:rufino/modules/employee/domain/model/address/address.dart';
import 'package:rufino/modules/employee/domain/model/contact/contact.dart';
import 'package:rufino/modules/employee/domain/model/employee.dart';
import 'package:rufino/modules/employee/domain/model/employee_with_role.dart';
import 'package:http/http.dart' as http;
import 'package:rufino/modules/employee/domain/model/id_card/id_card.dart';
import 'package:rufino/modules/employee/domain/model/personal_info/personal_info.dart';
import 'package:rufino/modules/employee/domain/model/personal_info/personal_info_seletion_options.dart';
import 'package:rufino/modules/employee/domain/model/status.dart';
import 'package:rufino/modules/employee/domain/model/workplace/workplace.dart';
import 'package:rufino/modules/employee/domain/model/role_info/department.dart';
import 'package:rufino/modules/employee/domain/model/role_info/position.dart';
import 'package:rufino/modules/employee/domain/model/role_info/role.dart';
import 'package:rufino/modules/employee/domain/model/role_info/role_info.dart';
import 'package:rufino/shared/services/base_service.dart';
import 'package:rufino/shared/util/data_convetion.dart';

class PeopleManagementService extends BaseService {
  final Uri peopleManagementUrl =
      Uri.https(const String.fromEnvironment("people_management_url"));

  PeopleManagementService(super._authService);

  Future<String> createEmployee(String companyId, String name) async {
    var headers = await getHeadersWithRequestId();
    Map<String, String> body = {
      "name": name,
    };
    var url = peopleManagementUrl.replace(path: "/api/v1/$companyId/employee");

    var response =
        await http.post(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> editEmployeeName(
      String employeeId, String companyId, String name) async {
    var headers = await getHeadersWithRequestId();
    Map<String, String> body = {
      "employeeId": employeeId,
      "name": name,
    };
    var url =
        peopleManagementUrl.replace(path: "/api/v1/$companyId/employee/name");

    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> editDocumentSigningOptions(String employeeId, String companyId,
      DocumentSigningOptions option) async {
    var headers = await getHeadersWithRequestId();
    Map<String, String> body = {
      "employeeId": employeeId,
      "documentSigningOptions": option.id,
    };
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/DocumentSigningOptions");

    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> editEmployeeContact(String employeeId, String companyId,
      String cellphone, String email) async {
    var headers = await getHeadersWithRequestId();
    Map<String, String> body = {
      "employeeId": employeeId,
      "cellphone": cellphone,
      "email": email,
    };
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/contact");

    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> editEmployeeAddress(
      String employeeId, String companyId, Address address) async {
    var headers = await getHeadersWithRequestId();
    Map<String, String> body = address.toJson();
    body.addAll({
      "employeeId": employeeId,
    });

    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/address");

    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> editEmployeePersonalInfo(
      String employeeId, String companyId, PersonalInfo personalInfo) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = personalInfo.toJson(employeeId);

    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/personalinfo");

    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> editEmployeeIdCard(
      String employeeId, String companyId, IdCard idCard) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = idCard.toJson(employeeId: employeeId);

    var url =
        peopleManagementUrl.replace(path: "/api/v1/$companyId/employee/idcard");

    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> editEmployeeVoteId(
      String employeeId, String companyId, VoteId value) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = value.toJson(employeeId);

    var url =
        peopleManagementUrl.replace(path: "/api/v1/$companyId/employee/voteid");

    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> editEmployeeMilitaryDocument(
      String employeeId, String companyId, MilitaryDocument value) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = value.toJson(employeeId);

    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/MilitaryDocument");

    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> editEmployeeDependent(
      String employeeId, String companyId, Dependent value) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = value.toJsonUpdateDependent(employeeId);

    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/dependent/edit");

    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> createEmployeeDependent(
      String employeeId, String companyId, Dependent value) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = value.toJsonCreateDependent(employeeId);

    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/dependent/create");

    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> editMedicalAdmissionExam(
      String employeeId, String companyId, MedicalAdmissionExam value) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = value.toJson(employeeId);

    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/medicaladmissionexam");

    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> editRoleInfo(
      String employeeId, String companyId, String roleId) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = {
      "employeeId": employeeId,
      "roleId": roleId,
    };

    var url =
        peopleManagementUrl.replace(path: "/api/v1/$companyId/employee/role");

    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> editWorkplace(
      String employeeId, String companyId, String workplaceId) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = {
      "employeeId": employeeId,
      "workPlaceId": workplaceId,
    };

    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/workplace");

    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> finishedContract(
      String employeeId, String companyId, String finalDate) async {
    var headers = await getHeadersWithRequestId();

    // Parse the input date string in dd/MM/yyyy format
    DateTime parsedDate = DateFormat('dd/MM/yyyy').parse(finalDate);

    // Format the parsed date to yyyy-MM-dd
    String formattedDate = DateFormat('yyyy-MM-dd').format(parsedDate);

    Map<String, dynamic> body = {
      "employeeId": employeeId,
      "finishDateContract": formattedDate,
    };

    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/contract/finished");

    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> newContract(String employeeId, String companyId,
      String initDate, String contractTypeId, String registration) async {
    var headers = await getHeadersWithRequestId();

    Map<String, dynamic> body = {
      "employeeId": employeeId,
      "registration": registration,
      "dateInit": DataConvetion.convertToDataOnly(initDate),
      "contractType": int.parse(contractTypeId),
    };

    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/admission/complete");

    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> removeEmployeeDependent(
      String employeeId, String companyId, String nameDependent) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = {
      "employeeId": employeeId,
      "nameDepedent": nameDependent
    };

    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/dependent/remove");

    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> addArchiveCategoryEvent(
      String companyId, String archiveCategoryId, List<int> eventList) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = {
      "archiveCategoryId": archiveCategoryId,
      "eventId": eventList,
    };

    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/archivecategory/event/add");

    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> removeArchiveCategoryEvent(
      String companyId, String archiveCategoryId, List<int> eventList) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = {
      "archiveCategoryId": archiveCategoryId,
      "eventId": eventList,
    };

    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/archivecategory/event/remove");

    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> editDescriptionArchiveCategory(
      String companyId, String archiveCategoryId, String description) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = {
      "archiveCategoryId": archiveCategoryId,
      "description": description,
    };

    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/archivecategory/description");

    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> createArchiveCategory(
      String companyId, ArchiveCategory category) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = {
      "name": category.name,
      "description": category.description.value,
      "listenEventsIds": [],
    };
    var url =
        peopleManagementUrl.replace(path: "/api/v1/$companyId/archivecategory");
    var response =
        await http.post(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> createRequireDocument(RequireDocument requireDocument) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = requireDocument.toJsonCreate();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/${requireDocument.companyId}/requiredocuments");
    var response =
        await http.post(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<String> editRequireDocument(RequireDocument requireDocument) async {
    var headers = await getHeadersWithRequestId();
    Map<String, dynamic> body = requireDocument.toJson();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/${requireDocument.companyId}/requiredocuments");
    var response =
        await http.put(url, headers: headers, body: jsonEncode(body));
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return jsonResponse["id"];
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<Employee> getEmployee(String id, String companyId) async {
    var url =
        peopleManagementUrl.replace(path: "/api/v1/$companyId/employee/$id");

    var headers = await getHeaders();

    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return Employee.fromJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<Contact> getEmployeeContact(String id, String companyId) async {
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/contact/$id");

    var headers = await getHeaders();

    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return Contact.fromJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<MedicalAdmissionExam> getEmployeeMedicalAdmissionExam(
      String id, String companyId) async {
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/medicaladmissionexam/$id");

    var headers = await getHeaders();

    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return MedicalAdmissionExam.fromJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<Address> getEmployeeAddress(String id, String companyId) async {
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/address/$id");

    var headers = await getHeaders();

    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return Address.fromJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<PersonalInfo> getEmployeePersonalInfo(
      String id, String companyId) async {
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/personalinfo/$id");

    var headers = await getHeaders();

    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return PersonalInfo.fromJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<IdCard> getEmployeeIdCard(String id, String companyId) async {
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/idcard/$id");

    var headers = await getHeaders();

    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return IdCard.fromJson(jsonResponse["idCard"]);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<VoteId> getEmployeeVoteId(String id, String companyId) async {
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/voteid/$id");

    var headers = await getHeaders();

    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return VoteId.fromJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<MilitaryDocument> getEmployeeMilitaryDocument(
      String id, String companyId) async {
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/militarydocument/$id");

    var headers = await getHeaders();

    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return MilitaryDocument.fromJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<Dependent>> getEmployeeDependents(
      String id, String companyId) async {
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/dependents/$id");

    var headers = await getHeaders();

    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return Dependent.fromListJson(jsonResponse["dependents"]);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<EmployeeWithRole>> getEmployees(
      String companyId,
      String? name,
      String? role,
      int? status,
      int sortOrder,
      int pageSize,
      int sizeSkip) async {
    Map<String, dynamic> queryParams = {
      "name": name,
      "role": role,
      "status": status,
      "sortOrder": sortOrder,
      "pageSize": pageSize,
      "sizeSkip": sizeSkip
    }..removeWhere((key, value) => value == null);

    queryParams =
        queryParams.map((key, value) => MapEntry(key, value.toString()));

    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/list", queryParameters: queryParams);

    var headers = await getHeaders();

    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      List<dynamic> jsonResponse = jsonDecode(response.body);
      return EmployeeWithRole.fromListJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<Status>> getStatus(String companyId) async {
    var headers = await getHeaders();
    var url =
        peopleManagementUrl.replace(path: "/api/v1/$companyId/employee/status");
    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      List<dynamic> jsonResponse = jsonDecode(response.body);
      return Status.fromListJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<DocumentSigningOptions>> getDocumentSigningOptions(
      String companyId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/DocumentSigningOptions");
    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      List<dynamic> jsonResponse = jsonDecode(response.body);
      return DocumentSigningOptions.fromListJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<require_document.Event>> getEvents(String companyId) async {
    var headers = await getHeaders();
    var url =
        peopleManagementUrl.replace(path: "/api/v1/$companyId/employee/events");
    var response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      List<dynamic> jsonResponse = jsonDecode(response.body);
      return require_document.Event.fromListJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<require_document.Event>> getRequireDocumentEvents(
      String companyId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/requiredocuments/events");
    var response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      List<dynamic> jsonResponse = jsonDecode(response.body);
      return require_document.Event.fromListJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<PersonalInfoSeletionOptions> getPersonalInfoSeletionOptions(
      String companyId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/personalinfo/selectionoptions");
    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return PersonalInfoSeletionOptions.fromJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<DependencyType>> getDependencyType(String companyId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/dependencytype");
    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      List<dynamic> jsonResponse = jsonDecode(response.body);
      return DependencyType.fromListJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<Gender>> getGender(String companyId) async {
    var headers = await getHeaders();
    var url =
        peopleManagementUrl.replace(path: "/api/v1/$companyId/employee/gender");
    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      List<dynamic> jsonResponse = jsonDecode(response.body);
      return Gender.fromListJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<RoleInfo> getRole(String companyId, String roleId) async {
    var headers = await getHeaders();
    var url =
        peopleManagementUrl.replace(path: "/api/v1/$companyId/role/$roleId");
    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      dynamic jsonResponse = jsonDecode(response.body);
      return RoleInfo.fromJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<Department>> getAllDepartment(String companyId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/department/all/simple");
    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      dynamic jsonResponse = jsonDecode(response.body);
      return Department.fromListJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<Position>> getAllPosition(
      String departmentId, String companyId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/position/all/simple/$departmentId");
    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      dynamic jsonResponse = jsonDecode(response.body);
      return Position.fromListJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<Role>> getAllRole(String positionId, String companyId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/role/all/simple/$positionId");
    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      dynamic jsonResponse = jsonDecode(response.body);
      return Role.fromListJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<Workplace> getWorkplaceById(
      String workplaceId, String companyId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/workplace/$workplaceId");
    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      dynamic jsonResponse = jsonDecode(response.body);
      return Workplace.fromJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<Workplace>> getAllWorkplace(String companyId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(path: "/api/v1/$companyId/workplace");
    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      dynamic jsonResponse = jsonDecode(response.body);
      return Workplace.fromListJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<EmployeeContract>> getEmployeeContracts(
      String employeeId, String companyId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/contracts/$employeeId");
    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      dynamic jsonResponse = jsonDecode(response.body);
      return EmployeeContract.fromListJson(jsonResponse["contracts"]);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<EmployeeContractType>> getEmployeeContractTypes(
      String companyId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/employee/contracts/types");
    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      List<dynamic> jsonResponse = jsonDecode(response.body);
      return EmployeeContractType.fromListJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<ArchiveCategory>> getArchiveCategories(String companyId) async {
    var headers = await getHeaders();
    var url =
        peopleManagementUrl.replace(path: "/api/v1/$companyId/archivecategory");
    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      List<dynamic> jsonResponse = jsonDecode(response.body);
      return ArchiveCategory.fromListJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<archive.Event>> getArchiveEvents(String companyId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/archivecategory/event");
    var response = await http.get(url, headers: headers);
    if (response.statusCode == 200) {
      List<dynamic> jsonResponse = jsonDecode(response.body);
      return archive.Event.fromListJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<RequireDocumentSimple>> getAllRequireDocumentsSimple(
      String companyId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/requiredocuments");

    var response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return RequireDocumentSimple.fromListJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<RequireDocument> getByIdRequireDocuments(
      String requireDocumentId, String companyId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/requiredocuments/$requireDocumentId");

    var response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return RequireDocument.fromJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<AssociationType>> getAllAssociationTypes(String companyId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path: "/api/v1/$companyId/requiredocuments/associationtype");

    var response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return AssociationType.fromListJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }

  Future<List<Association>> getAllAssociation(
      String companyId, String associationTypeId) async {
    var headers = await getHeaders();
    var url = peopleManagementUrl.replace(
        path:
            "/api/v1/$companyId/requiredocuments/association/$associationTypeId");

    var response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      var jsonResponse = jsonDecode(response.body);
      return Association.fromListJson(jsonResponse);
    }
    throw treatUnsuccessfulResponses(response);
  }
}
