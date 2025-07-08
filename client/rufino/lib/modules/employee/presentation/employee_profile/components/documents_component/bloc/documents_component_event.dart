part of 'documents_component_bloc.dart';

sealed class DocumentsComponentEvent extends Equatable {
  const DocumentsComponentEvent();

  @override
  List<Object> get props => [];
}

class InitialEvent extends DocumentsComponentEvent {
  final String companyId;
  final String employeeId;
  const InitialEvent({
    required this.companyId,
    required this.employeeId,
  });
  @override
  List<Object> get props => [companyId, employeeId];
}

class ExpandEvent extends DocumentsComponentEvent {
  const ExpandEvent();
  @override
  List<Object> get props => [];
}

class ExpandDocumentEvent extends DocumentsComponentEvent {
  final String documentId;
  final bool isExpanded;
  const ExpandDocumentEvent(this.documentId, this.isExpanded);
  @override
  List<Object> get props => [documentId];
}

class SnackMessageWasShowEvent extends DocumentsComponentEvent {}

class EditDocumentUnitEvent extends DocumentsComponentEvent {
  final String date;
  final String documentId;
  final String documentUnitId;

  const EditDocumentUnitEvent(this.date, this.documentId, this.documentUnitId);
  @override
  List<Object> get props => [date, documentId, documentUnitId];
}

class CreateDocumentUnitEvent extends DocumentsComponentEvent {
  final String documentId;

  const CreateDocumentUnitEvent(this.documentId);
  @override
  List<Object> get props => [documentId];
}

class RefeshEvent extends DocumentsComponentEvent {
  final String documentId;
  const RefeshEvent(this.documentId);
  @override
  List<Object> get props => [documentId];
}

class GenerateDocumentUnitEvent extends DocumentsComponentEvent {
  final String documentId;
  final String documentUnitId;
  const GenerateDocumentUnitEvent(this.documentId, this.documentUnitId);
  @override
  List<Object> get props => [documentId, documentUnitId];
}

class GenerateAndSend2SignEvent extends DocumentsComponentEvent {
  final String dateLimitToSign;
  final String eminderEveryNDays;
  final String documentId;
  final String documentUnitId;

  const GenerateAndSend2SignEvent(
    this.dateLimitToSign,
    this.eminderEveryNDays,
    this.documentId,
    this.documentUnitId,
  );
  @override
  List<Object> get props =>
      [documentId, documentUnitId, dateLimitToSign, eminderEveryNDays];
}

class LoadDocumentUnitEvent extends DocumentsComponentEvent {
  final String documentId;
  final String documentUnitId;
  const LoadDocumentUnitEvent(this.documentUnitId, this.documentId);
  @override
  List<Object> get props => [documentUnitId, documentId];
}

class LoadDocumentUnitToSignEvent extends DocumentsComponentEvent {
  final String dateLimitToSign;
  final String eminderEveryNDays;
  final String documentId;
  final String documentUnitId;
  const LoadDocumentUnitToSignEvent(this.dateLimitToSign,
      this.eminderEveryNDays, this.documentUnitId, this.documentId);
  @override
  List<Object> get props =>
      [documentUnitId, documentId, dateLimitToSign, eminderEveryNDays];
}

class DownloadDocumentUnitEvent extends DocumentsComponentEvent {
  final String documentId;
  final String documentUnitId;
  const DownloadDocumentUnitEvent(this.documentId, this.documentUnitId);
  @override
  List<Object> get props => [documentId, documentUnitId];
}
