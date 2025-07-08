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
    var result = <int>[];
    for (var e in list) {
      if (e.toInt() == 0) {
        continue; // Assuming 0 is the empty type
      }
      result.add(e.toInt());
    }
    return result;
  }

  bool get isEmpty {
    return list.isEmpty;
  }

  bool get isNotEmpty {
    return list.isNotEmpty;
  }
}
