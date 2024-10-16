import 'package:rufino/modules/employee/domain/model/base/enumeration_list.dart';
import 'package:rufino/modules/employee/domain/model/personal_info/disability.dart';
import 'package:rufino/modules/employee/domain/model/personal_info/observation.dart';

class Deficiency extends EnumerationList<Disability> {
  final Observation observation;

  const Deficiency(this.observation, List<Disability> list)
      : super("Deficiência", list);

  const Deficiency.empty({this.observation = const Observation.empty()})
      : super("Deficiência", const []);

  static Deficiency fromJson(Map<String, dynamic> json) {
    return Deficiency(Observation(json["observation"]),
        Disability.fromListJson(json["disabilities"]));
  }

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
