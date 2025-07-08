import 'package:equatable/equatable.dart';

class TextBase extends Equatable {
  final String displayName;
  final String value;

  const TextBase(this.displayName, this.value);

  @override
  List<Object?> get props => [value];
}
