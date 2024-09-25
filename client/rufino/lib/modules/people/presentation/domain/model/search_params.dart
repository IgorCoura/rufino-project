import 'package:equatable/equatable.dart';

class SearchParam extends Equatable {
  final int id;
  final String value;

  const SearchParam._(this.id, this.value);

  static const SearchParam name = SearchParam._(1, "Nome");
  static const SearchParam role = SearchParam._(2, "Função");

  static SearchParam? fromId(int id) {
    return values.firstWhere((param) => param.id == id,
        orElse: () => throw StateError(
            "error not trying to convert id: $id to SearchParam"));
  }

  @override
  String toString() {
    return value;
  }

  @override
  List<Object?> get props => [id, name];

  static const List<SearchParam> values = [name, role];
}
