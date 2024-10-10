import 'package:equatable/equatable.dart';

abstract class Enumeration extends Equatable {
  final int id;
  final String name;
  final String displayName;

  const Enumeration(this.id, this.name, this.displayName);

  static int get emptyId => -1;
  static String get emptyName => "";

  @override
  List<Object?> get props => [id, name];
}
