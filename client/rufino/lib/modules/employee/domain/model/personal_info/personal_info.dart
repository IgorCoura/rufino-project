import 'package:rufino/modules/employee/domain/model/model_base.dart';
import 'package:rufino/modules/employee/domain/model/personal_info/deficiency.dart';
import 'package:rufino/modules/employee/domain/model/prop_base.dart';

class PersonalInfo extends ModelBase {
  final Deficiency deficiency;

  const PersonalInfo(this.deficiency, {super.isLoading = false});

  static PersonalInfo get loading =>
      PersonalInfo(Deficiency.loading, isLoading: true);

  factory PersonalInfo.fromJson(Map<String, dynamic> json) {
    return PersonalInfo(Deficiency.fromJson(json["deficiency"]),
        isLoading: false);
  }

  @override
  List<PropBase> get props => [];

  @override
  List<ModelBase> get models => [deficiency];
}
