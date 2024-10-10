import 'package:rufino/modules/employee/domain/model/enumeration_list.dart';
import 'package:rufino/modules/employee/domain/model/personal_info/Disability.dart';
import 'package:rufino/modules/employee/domain/model/personal_info/observation.dart';

class Deficiency extends EnumerationList<Disability> {
  final Observation observation;

  const Deficiency(this.observation, List<Disability> list)
      : super("Deficiência", list);

  static Deficiency fromJson(Map<String, dynamic> json) {
    return Deficiency(Observation(json["observation"]),
        Disability.fromListJson(json["disabilities"]));
  }

  static Deficiency get empty => Deficiency(Observation.empty, List.empty());

  @override
  Deficiency copyWith(
      {List<Disability>? list, Observation? observation, Object? generic}) {
    if (generic != null) {
      switch (generic.runtimeType) {
        case const (Observation):
          observation = generic as Observation?;
        case const (List<Disability>):
          list = generic as List<Disability>?;
      }
    }

    return Deficiency(observation ?? this.observation, list ?? this.list);
  }

  Map<String, dynamic> toJson() {
    return {
      "disability": list.map((el) => el.id).toList(),
      "observation": observation.value
    };
  }
}
