import 'package:equatable/equatable.dart';

abstract class Enumeration extends Equatable {
  static const int emptyId = -1;
  static const String emptyName = "";
  final int id;
  final String name;
  final String displayName;

  const Enumeration(this.id, this.name, this.displayName);
  const Enumeration.empty(
      {this.id = emptyId, this.name = emptyName, this.displayName = ""});

  @override
  List<Object?> get props => [id, name];
}
