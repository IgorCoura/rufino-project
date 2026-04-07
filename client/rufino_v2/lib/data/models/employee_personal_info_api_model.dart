import '../../domain/entities/employee_personal_info.dart';
import '../../domain/entities/personal_info_options.dart';
import '../../domain/entities/selection_option.dart';

// ─── Translation maps ─────────────────────────────────────────────────────────
// The API returns option names in English or without accents. These maps
// translate each option id to its Brazilian Portuguese display name, matching
// the same conversions used in the legacy rufino app.

const _genderNames = {
  '1': 'Homem',
  '2': 'Mulher',
};

const _maritalStatusNames = {
  '1': 'Solteiro(a)',
  '2': 'Casado(a)',
  '3': 'Divorciado(a)',
  '4': 'Viúvo(a)',
};

const _ethnicityNames = {
  '1': 'Branco',
  '2': 'Negro',
  '3': 'Pardo',
  '4': 'Amarelo',
  '5': 'Indígena',
  '6': 'Não declarado',
};

const _educationLevelNames = {
  '1': 'Analfabeto',
  '2': 'Ensino Fundamental Incompleto',
  '3': 'Ensino Fundamental Completo',
  '4': 'Ensino Médio Incompleto',
  '5': 'Ensino Médio Completo',
  '6': 'Ensino Superior Incompleto',
  '7': 'Ensino Superior Completo',
  '8': 'Pós-Graduação Completo',
  '9': 'Mestrado Completo',
  '10': 'Doutorado Completo',
};

const _disabilityNames = {
  '1': 'Física',
  '2': 'Intelectual',
  '3': 'Mental',
  '4': 'Auditiva',
  '5': 'Visual',
  '6': 'Reabilitado',
  '7': 'Cota de Incapacidade',
};

// ─── Models ───────────────────────────────────────────────────────────────────

/// Data Transfer Object for a single id/name option returned by the API.
class SelectionOptionApiModel {
  /// Creates a [SelectionOptionApiModel] from the given fields.
  const SelectionOptionApiModel({required this.id, required this.name});

  /// The option identifier.
  final String id;

  /// The human-readable option name.
  final String name;

  /// Converts this model to a domain [SelectionOption] entity.
  SelectionOption toEntity() => SelectionOption(id: id, name: name);
}

/// Data Transfer Object for the employee personal info endpoint.
class EmployeePersonalInfoApiModel {
  /// Creates an [EmployeePersonalInfoApiModel] from the given fields.
  const EmployeePersonalInfoApiModel({
    required this.genderId,
    required this.maritalStatusId,
    required this.ethnicityId,
    required this.educationLevelId,
    required this.disabilityIds,
    required this.disabilityObservation,
  });

  /// The id of the gender option.
  final String genderId;

  /// The id of the marital status option.
  final String maritalStatusId;

  /// The id of the ethnicity option.
  final String ethnicityId;

  /// The id of the education level option.
  final String educationLevelId;

  /// The list of disability option ids.
  final List<String> disabilityIds;

  /// Additional disability observation text.
  final String disabilityObservation;

  /// Deserialises an [EmployeePersonalInfoApiModel] from the API JSON map.
  ///
  /// The JSON structure nests gender, maritalStatus, ethinicity, educationLevel
  /// and deficiency as objects with id/name fields.
  factory EmployeePersonalInfoApiModel.fromJson(Map<String, dynamic> json) {
    String parseOptionId(dynamic raw) {
      if (raw == null) return '';
      if (raw is Map<String, dynamic>) return raw['id']?.toString() ?? '';
      return '';
    }

    final deficiency = json['deficiency'] as Map<String, dynamic>?;
    // The GET response uses the plural key "disabilities"; the PUT body uses
    // the singular "disability". Fall back to singular for resilience.
    final rawDisabilities = deficiency != null
        ? (deficiency['disabilities'] ?? deficiency['disability'])
            as List<dynamic>?
        : null;
    final disabilityIds = rawDisabilities
            ?.map((d) => (d as Map<String, dynamic>)['id'].toString())
            .toList() ??
        <String>[];
    final observation =
        deficiency != null ? deficiency['observation'] as String? ?? '' : '';

    return EmployeePersonalInfoApiModel(
      genderId: parseOptionId(json['gender']),
      maritalStatusId: parseOptionId(json['maritalStatus']),
      ethnicityId: parseOptionId(json['ethinicity']),
      educationLevelId: parseOptionId(json['educationLevel']),
      disabilityIds: disabilityIds,
      disabilityObservation: observation,
    );
  }

  /// Converts this model to a domain [EmployeePersonalInfo] entity.
  EmployeePersonalInfo toEntity() {
    return EmployeePersonalInfo(
      genderId: genderId,
      maritalStatusId: maritalStatusId,
      ethnicityId: ethnicityId,
      educationLevelId: educationLevelId,
      disabilityIds: disabilityIds,
      disabilityObservation: disabilityObservation,
    );
  }
}

/// Data Transfer Object for the personal info selection options endpoint.
class PersonalInfoOptionsApiModel {
  /// Creates a [PersonalInfoOptionsApiModel] with all option lists.
  const PersonalInfoOptionsApiModel({
    required this.genders,
    required this.maritalStatuses,
    required this.ethnicities,
    required this.educationLevels,
    required this.disabilities,
  });

  /// Available gender options.
  final List<SelectionOptionApiModel> genders;

  /// Available marital status options.
  final List<SelectionOptionApiModel> maritalStatuses;

  /// Available ethnicity options.
  final List<SelectionOptionApiModel> ethnicities;

  /// Available education level options.
  final List<SelectionOptionApiModel> educationLevels;

  /// Available disability options.
  final List<SelectionOptionApiModel> disabilities;

  /// Deserialises a [PersonalInfoOptionsApiModel] from the API JSON map.
  ///
  /// Each option name is translated to Brazilian Portuguese using a local
  /// translation map keyed by option id. If an id is not found in the map,
  /// the server-provided name is used as a fallback.
  factory PersonalInfoOptionsApiModel.fromJson(Map<String, dynamic> json) {
    List<SelectionOptionApiModel> parseList(
      dynamic raw,
      Map<String, String> translations,
    ) {
      if (raw == null) return [];
      return (raw as List<dynamic>).map((e) {
        final map = e as Map<String, dynamic>;
        final id = map['id']?.toString() ?? '';
        final name = translations[id] ?? map['name'] as String? ?? '';
        return SelectionOptionApiModel(id: id, name: name);
      }).toList();
    }

    return PersonalInfoOptionsApiModel(
      genders: parseList(json['gender'], _genderNames),
      maritalStatuses: parseList(json['maritalStatus'], _maritalStatusNames),
      ethnicities: parseList(json['ethinicity'], _ethnicityNames),
      educationLevels: parseList(json['educationLevel'], _educationLevelNames),
      disabilities: parseList(json['disability'], _disabilityNames),
    );
  }

  /// Converts this model to a domain [PersonalInfoOptions] entity.
  PersonalInfoOptions toEntity() {
    return PersonalInfoOptions(
      genders: genders.map((e) => e.toEntity()).toList(),
      maritalStatuses: maritalStatuses.map((e) => e.toEntity()).toList(),
      ethnicities: ethnicities.map((e) => e.toEntity()).toList(),
      educationLevels: educationLevels.map((e) => e.toEntity()).toList(),
      disabilities: disabilities.map((e) => e.toEntity()).toList(),
    );
  }
}
