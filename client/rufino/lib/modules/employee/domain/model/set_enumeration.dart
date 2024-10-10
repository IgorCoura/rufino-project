import 'package:equatable/equatable.dart';
import 'package:rufino/modules/employee/domain/model/enumeration.dart';

class SetEnumeration<T extends Enumeration> extends Equatable {
  final T? selected;
  final List<T> list;
  final String diplayName;

  const SetEnumeration(this.selected, this.list, this.diplayName);

  @override
  List<Object?> get props => [selected, list.hashCode];
}
