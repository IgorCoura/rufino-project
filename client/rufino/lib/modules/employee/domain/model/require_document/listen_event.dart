import 'package:equatable/equatable.dart';
import 'package:rufino/modules/employee/domain/model/require_document/event.dart';
import 'package:rufino/modules/employee/domain/model/require_document/status.dart';

class ListenEvent extends Equatable {
  final Event event;
  final List<Status> statusList;

  const ListenEvent(this.event, this.statusList);

  ListenEvent.empty()
      : event = Event.empty(),
        statusList = [];

  ListenEvent copyWith({
    Event? event,
    List<Status>? statusList,
  }) {
    return ListenEvent(
      event ?? this.event,
      statusList ?? this.statusList,
    );
  }

  factory ListenEvent.fromJson(Map<String, dynamic> json) {
    return ListenEvent(
      Event.fromJson(json['event']),
      (json['status'] as List<dynamic>)
          .map((status) => Status.fromNumber(status))
          .toList(),
    );
  }

  static List<ListenEvent> fromJsonList(List<dynamic> jsonList) {
    return jsonList.map((json) => ListenEvent.fromJson(json)).toList();
  }

  Map<String, dynamic> toJson() {
    return {
      'eventId': event.id,
      'status': statusList.map((status) => status.id).toList(),
    };
  }

  @override
  List<Object?> get props => [event, statusList.hashCode];
}
