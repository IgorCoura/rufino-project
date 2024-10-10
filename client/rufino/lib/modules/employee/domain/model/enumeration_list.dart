import 'package:equatable/equatable.dart';
import 'package:rufino/modules/employee/domain/model/enumeration.dart';

abstract class EnumerationList<T extends Enumeration> extends Equatable {
  final String displayName;
  final List<T> list;
  const EnumerationList(this.displayName, this.list);

  EnumerationList copyWith({List<T>? list});

  EnumerationList removeItem(T item) {
    List<T> list = List.from(this.list);
    list.removeWhere((x) => x.id == item.id);
    var newEnum = this.copyWith(list: list);
    return newEnum;
  }

  EnumerationList addItem(T item) {
    if (this.list.any((x) => x.id == item.id)) {
      return this;
    }
    List<T> list = List.from(this.list);
    list.add(item);
    var newEnum = this.copyWith(list: list);
    return newEnum;
  }

  @override
  List<Object?> get props => [list.hashCode];
}
