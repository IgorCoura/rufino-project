import 'package:equatable/equatable.dart';

class Username extends Equatable {
  final String value;

  const Username(this.value);

  bool isValid() {
    return value.isNotEmpty && value.length >= 3;
  }

  @override
  String toString() {
    return value;
  }

  @override
  List<Object?> get props => [value];

  static const empty = Username('');
}
