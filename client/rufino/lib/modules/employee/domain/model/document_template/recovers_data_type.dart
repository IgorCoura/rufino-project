import 'package:rufino/modules/employee/domain/model/base/enumeration_list.dart';
import 'package:rufino/modules/employee/domain/model/document_template/recover_data_type.dart';

class RecoversDataType extends EnumerationList<RecoverDataType> {
  const RecoversDataType(List<RecoverDataType> list)
      : super("Tipos de recuperação de dados", list);

  const RecoversDataType.empty()
      : super("Tipos de recuperação de dados", const [RecoverDataType.empty()]);

  @override
  RecoversDataType copyWith({List<RecoverDataType>? list}) {
    return RecoversDataType(list ?? this.list);
  }

  factory RecoversDataType.fromJson(Map<String, dynamic> json) {
    return RecoversDataType(
        RecoverDataType.fromListJson(json['recoversDataType']));
  }

  List<int> toJson() {
    return list.map((e) => e.toInt()).toList();
  }

  bool get isEmpty {
    return list.isEmpty;
  }

  bool get isNotEmpty {
    return list.isNotEmpty;
  }
}
