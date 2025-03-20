import 'package:equatable/equatable.dart';
import 'package:rufino/modules/employee/domain/role/role_name.dart';

class Role extends Equatable {
  final bool isLoading;
  final bool isLazyLoading;
  final RoleName roleName;

  const Role(this.roleName,
      {this.isLoading = false, this.isLazyLoading = false});

  const Role.loading(
      {this.roleName = const RoleName.empty(),
      this.isLoading = true,
      this.isLazyLoading = false});

  Role copyWith(
      {RoleName? roleName,
      Object? generic,
      bool? isLoading,
      bool? isLazyLoading}) {
    if (generic != null) {
      switch (generic.runtimeType) {
        case const (RoleName):
          roleName = generic as RoleName?;
      }
    }
    return Role(
      roleName ?? this.roleName,
      isLoading: isLoading ?? this.isLoading,
      isLazyLoading: isLazyLoading ?? this.isLazyLoading,
    );
  }

  factory Role.fromJson(Map<String, dynamic> json) {
    return Role(RoleName.fromJson(json));
  }

  Map<String, dynamic> toJson() {
    return {
      "id": roleName.id,
      "name": roleName.name,
    };
  }

  @override
  List<Object?> get props => [
        isLoading,
        isLazyLoading,
        roleName,
      ];
}
