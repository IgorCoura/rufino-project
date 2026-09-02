/// Contas a pagar: captura, verificação, aprovação e expectativas.
///
/// A casca do app enxerga este módulo por um contrato pequeno — rotas,
/// providers e entradas de menu —, e nada além dele. É o que permite ligar ou
/// desligar o produto sem tocar no outro.
library;

export 'src/bill_payment_permissions.dart';
export 'src/data/artifact_response.dart';
export 'src/data/bill_api_service.dart';
export 'src/data/bill_repository_impl.dart';
export 'src/data/capture_item_api_service.dart';
export 'src/data/captured_message_api_service.dart';
export 'src/data/captured_message_repository_impl.dart';
export 'src/data/capture_item_repository_impl.dart';
export 'src/data/capture_source_api_service.dart';
export 'src/data/capture_source_repository_impl.dart';
export 'src/data/expectation_api_service.dart';
export 'src/data/expectation_repository_impl.dart';
export 'src/data/payee_api_service.dart';
export 'src/data/payee_repository_impl.dart';
export 'src/data/payer_profile_api_service.dart';
export 'src/data/payer_profile_repository_impl.dart';
export 'src/data/payment_api_service.dart';
export 'src/data/payment_repository_impl.dart';
export 'src/data/trusted_origin_api_service.dart';
export 'src/data/trusted_origin_repository_impl.dart';
export 'src/domain/bill.dart';
export 'src/domain/bill_check.dart';
export 'src/domain/bill_detail.dart';
export 'src/domain/bill_payment_enums.dart';
export 'src/domain/bill_payment_exception.dart';
export 'src/domain/bill_repository.dart';
export 'src/domain/capture_item.dart';
export 'src/domain/capture_item_repository.dart';
export 'src/domain/capture_source.dart';
export 'src/domain/capture_source_repository.dart';
export 'src/domain/captured_artifact.dart';
export 'src/domain/captured_message.dart';
export 'src/domain/email_message.dart';
export 'src/domain/captured_message_repository.dart';
export 'src/domain/check_translations.dart';
export 'src/domain/expectation.dart';
export 'src/domain/expectation_repository.dart';
export 'src/domain/payee.dart';
export 'src/domain/payee_repository.dart';
export 'src/domain/payer_profile.dart';
export 'src/domain/payer_profile_repository.dart';
export 'src/domain/payment_order.dart';
export 'src/domain/payment_repository.dart';
export 'src/domain/trusted_origin.dart';
export 'src/domain/trusted_origin_repository.dart';
// O tipo do seletor de arquivos: a casca implementa (ela tem o `file_picker`),
// o módulo consome. Sem isto, quem monta as rotas não tem como nomear o callback.
export 'src/ui/shared/document_picker.dart'
    show DocumentPicker, LinkOpener, PickedDocument;
export 'src/ui/bill_payment_routes.dart';
