part of 'document_template_bloc.dart';

class DocumentTemplateState extends Equatable {
  final bool isSavingData;
  final bool isLoading;
  final bool isEditing;
  final AplicationException? exception;
  final String? snackMessage;
  final DocumentTemplate documentTemplate;
  final List<PlaceSignature> placeSignatures;
  final List<RecoverDataType> recoverDataType;
  final List<TypeSignature> typeSignature;
  final bool hasFile;

  const DocumentTemplateState({
    this.isLoading = false,
    this.isSavingData = false,
    this.isEditing = false,
    this.exception,
    this.snackMessage,
    this.documentTemplate = const DocumentTemplate.empty(),
    this.recoverDataType = const [],
    this.typeSignature = const [],
    this.placeSignatures = const [],
    this.hasFile = false,
  });

  DocumentTemplateState copyWith({
    bool? isLoading,
    bool? isSavingData,
    bool? isEditing,
    AplicationException? exception,
    String? snackMessage,
    DocumentTemplate? documentTemplate,
    List<PlaceSignature>? placeSignatures,
    List<RecoverDataType>? recoverDataType,
    List<TypeSignature>? typeSignature,
    bool? hasFile,
  }) {
    return DocumentTemplateState(
      isLoading: isLoading ?? this.isLoading,
      isSavingData: isSavingData ?? this.isSavingData,
      isEditing: isEditing ?? this.isEditing,
      exception: exception ?? this.exception,
      snackMessage: snackMessage ?? this.snackMessage,
      documentTemplate: documentTemplate ?? this.documentTemplate,
      recoverDataType: recoverDataType ?? this.recoverDataType,
      typeSignature: typeSignature ?? this.typeSignature,
      placeSignatures: placeSignatures ?? this.placeSignatures,
      hasFile: hasFile ?? this.hasFile,
    );
  }

  @override
  List<Object?> get props => [
        isSavingData,
        isLoading,
        isEditing,
        exception,
        snackMessage,
        documentTemplate,
        recoverDataType.hashCode,
        typeSignature.hashCode,
        placeSignatures.hashCode,
        hasFile,
      ];
}
