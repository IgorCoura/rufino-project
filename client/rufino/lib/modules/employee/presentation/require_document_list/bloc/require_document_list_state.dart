part of 'require_document_list_bloc.dart';

class RequireDocumentListState extends Equatable {
  final List<RequireDocumentSimple> requireDocumentList;
  final bool isLoading;
  final AplicationException? exception;

  const RequireDocumentListState(
      {this.requireDocumentList = const [],
      this.isLoading = false,
      this.exception});

  RequireDocumentListState copyWith({
    List<RequireDocumentSimple>? requireDocumentList,
    bool? isLoading,
    AplicationException? exception,
  }) {
    return RequireDocumentListState(
      requireDocumentList: requireDocumentList ?? this.requireDocumentList,
      isLoading: isLoading ?? this.isLoading,
      exception: exception ?? this.exception,
    );
  }

  @override
  List<Object?> get props =>
      [requireDocumentList.hashCode, isLoading, exception];
}
