import 'package:rufino/modules/employee/domain/model/archive_category/description.dart';
import 'package:rufino/modules/employee/domain/model/archive_category/listen_events.dart';
import 'package:rufino/modules/employee/domain/model/base/model_base.dart';

class ArchiveCategory extends ModelBase {
  final String id;
  final String name;
  final Description description;
  final ListenEvents listenEvents;

  const ArchiveCategory(this.id, this.name, this.description, this.listenEvents,
      {super.isLoading = false, super.isLazyLoading = false});
  const ArchiveCategory.empty(
      {this.id = "",
      this.name = "",
      this.description = const Description.empty(),
      this.listenEvents = const ListenEvents.empty()});

  ArchiveCategory copyWith({
    String? name,
    Description? description,
    ListenEvents? listenEvents,
    bool? isLoading,
    bool? isLazyLoading,
    Object? generic,
  }) {
    if (generic != null) {
      switch (generic.runtimeType) {
        case const (Description):
          description = generic as Description?;
        case const (ListenEvents):
          listenEvents = generic as ListenEvents?;
      }
    }
    return ArchiveCategory(
      id,
      name ?? this.name,
      description ?? this.description,
      listenEvents ?? this.listenEvents,
      isLoading: isLoading ?? this.isLoading,
      isLazyLoading: isLazyLoading ?? this.isLazyLoading,
    );
  }

  factory ArchiveCategory.fromJson(Map<String, dynamic> json) {
    return ArchiveCategory(
      json["id"],
      json['name'],
      Description(json['description']),
      ListenEvents.fromJson(json),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'description': description.value,
      'listenEvents': listenEvents.toJson(),
      'isLoading': isLoading,
      'isLazyLoading': isLazyLoading,
    };
  }

  static List<ArchiveCategory> fromListJson(List<dynamic> jsonList) {
    return jsonList.map((json) => ArchiveCategory.fromJson(json)).toList();
  }

  @override
  List<Object?> get props =>
      [name, description, listenEvents, isLoading, isLazyLoading];
}
