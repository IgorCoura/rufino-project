import 'package:rufino/modules/employee/domain/model/model_base.dart';
import 'package:rufino/modules/employee/domain/model/personal_info/observation.dart';
import 'package:rufino/modules/employee/domain/model/prop_base.dart';

class Deficiency extends ModelBase {
  final Observation observation;

  const Deficiency(this.observation, {super.isLoading = false});

  static Deficiency get loading =>
      Deficiency(Observation.empty, isLoading: true);

  factory Deficiency.fromJson(Map<String, dynamic> json) {
    return Deficiency(Observation(json["observation"]), isLoading: false);
  }

  @override
  List<ModelBase> get models => [];

  @override
  List<PropBase> get props => [observation];
}
