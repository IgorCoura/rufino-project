import 'package:equatable/equatable.dart';
import 'package:rufino/modules/employee/domain/model/role_info/department.dart';
import 'package:rufino/modules/employee/domain/model/role_info/position.dart';
import 'package:rufino/modules/employee/domain/model/role_info/role.dart';

class RoleInfo extends Equatable {
  final bool isLoading;
  final bool isLazyLoading;
  final Role role;
  final Position position;
  final Department department;

  const RoleInfo(this.role, this.position, this.department,
      {this.isLoading = false, this.isLazyLoading = false});

  const RoleInfo.loading(
      {this.role = const Role.empty(),
      this.position = const Position.empty(),
      this.department = const Department.empty(),
      this.isLoading = true,
      this.isLazyLoading = false});

  const RoleInfo.empty()
      : role = const Role.empty(),
        position = const Position.empty(),
        department = const Department.empty(),
        isLoading = false,
        isLazyLoading = false;

  RoleInfo copyWith(
      {Role? role,
      Position? position,
      Department? depertment,
      Object? generic,
      bool? isLoading,
      bool? isLazyLoading}) {
    if (generic != null) {
      switch (generic.runtimeType) {
        case const (Role):
          role = generic as Role?;
        case const (Position):
          position = generic as Position?;
        case const (Department):
          depertment = generic as Department?;
      }
    }
    return RoleInfo(
      role ?? this.role,
      position ?? this.position,
      depertment ?? this.department,
      isLoading: isLoading ?? this.isLoading,
      isLazyLoading: isLazyLoading ?? this.isLazyLoading,
    );
  }

  factory RoleInfo.fromJson(Map<String, dynamic> json) {
    return RoleInfo(Role.fromJson(json), Position.fromJson(json["position"]),
        Department.fromJson(json["position"]["department"]));
  }

  Map<String, dynamic> toJson() {
    return {
      "id": role.id,
      "name": role.name,
    };
  }

  @override
  List<Object?> get props => [
        isLoading,
        isLazyLoading,
        role,
        department,
        position,
      ];
}
