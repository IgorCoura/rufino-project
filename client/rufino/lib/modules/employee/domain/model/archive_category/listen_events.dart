import 'package:rufino/modules/employee/domain/model/archive_category/event.dart';
import 'package:rufino/modules/employee/domain/model/base/enumeration_list.dart';

class ListenEvents extends EnumerationList<Event> {
  const ListenEvents(List<Event> list) : super("Eventos", list);

  const ListenEvents.empty() : super("Eventos", const [Event.empty()]);

  static ListenEvents fromJson(Map<String, dynamic> json) {
    return ListenEvents(Event.fromListJson(json['listenEvents']));
  }

  @override
  ListenEvents copyWith({List<Event>? list, Object? generic}) {
    if (generic != null) {
      switch (generic.runtimeType) {
        case const (List<Event>):
          list = generic as List<Event>?;
      }
    }

    return ListenEvents(list ?? this.list);
  }

  Map<String, dynamic> toJson() {
    return {
      "listenEventsIds": list.map((el) => int.parse(el.id)).toList(),
    };
  }
}
