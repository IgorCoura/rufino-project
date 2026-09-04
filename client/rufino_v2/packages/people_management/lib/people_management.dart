/// Gestão de pessoas: funcionários, documentos, cargos e locais de trabalho.
///
/// A casca compõe; este pacote não conhece a casca. O contrato entre os dois é
/// só o que está exportado aqui: as rotas, os tipos que a casca precisa
/// instanciar (api services, repositórios e suas interfaces), o domínio, e os
/// typedefs das capacidades de plataforma que ele recebe de fora.
///
/// **Tela e ViewModel não são exportados de propósito.** Quem navega para uma
/// tela deste produto usa uma rota de `PeopleManagementRoutes`; quem precisa de
/// um dado usa um repositório. Exportar a UI transformaria cada tela em API
/// pública e faria a casca voltar a conhecer o produto por dentro.
library;

// Domínio — entidades ricas
export 'src/domain/entities/address.dart';
export 'src/domain/entities/batch_document_unit.dart';
export 'src/domain/entities/batch_download.dart';
export 'src/domain/entities/bulk_upload_match.dart';
export 'src/domain/entities/company.dart';
export 'src/domain/entities/company_detail.dart';
export 'src/domain/entities/department.dart';
export 'src/domain/entities/document_dashboard.dart';
export 'src/domain/entities/document_group.dart';
export 'src/domain/entities/document_group_with_documents.dart';
export 'src/domain/entities/document_group_with_templates.dart';
export 'src/domain/entities/document_template.dart';
export 'src/domain/entities/employee.dart';
export 'src/domain/entities/employee_contact.dart';
export 'src/domain/entities/employee_contract.dart';
export 'src/domain/entities/employee_dependent.dart';
export 'src/domain/entities/employee_document.dart';
export 'src/domain/entities/employee_id_card.dart';
export 'src/domain/entities/employee_medical_exam.dart';
export 'src/domain/entities/employee_military_document.dart';
export 'src/domain/entities/employee_personal_info.dart';
export 'src/domain/entities/employee_profile.dart';
export 'src/domain/entities/employee_social_integration_program.dart';
export 'src/domain/entities/employee_vote_id.dart';
export 'src/domain/entities/period.dart';
export 'src/domain/entities/personal_info_options.dart';
export 'src/domain/entities/position.dart';
export 'src/domain/entities/remuneration.dart';
export 'src/domain/entities/require_document.dart';
export 'src/domain/entities/role.dart';
export 'src/domain/entities/scanned_document.dart';
export 'src/domain/entities/selection_option.dart';
export 'src/domain/entities/signing_option.dart';
export 'src/domain/entities/workplace.dart';

// Domínio — contratos de repositório
export 'src/domain/repositories/batch_document_repository.dart';
export 'src/domain/repositories/batch_download_repository.dart';
export 'src/domain/repositories/cep_repository.dart';
export 'src/domain/repositories/company_repository.dart';
export 'src/domain/repositories/department_repository.dart';
export 'src/domain/repositories/document_dashboard_repository.dart';
export 'src/domain/repositories/document_group_repository.dart';
export 'src/domain/repositories/document_scanner_repository.dart';
export 'src/domain/repositories/document_template_repository.dart';
export 'src/domain/repositories/employee_repository.dart';
export 'src/domain/repositories/require_document_repository.dart';
export 'src/domain/repositories/workplace_repository.dart';

// A costura com a casca: o módulo (D6).
export 'src/people_management_module.dart';

// Permissões — os nomes de recurso e escopo do realm.
export 'src/people_management_permissions.dart';

// UI — só as rotas e as constantes de rota. Tela e ViewModel ficam privadas.
export 'src/ui/people_management_routes.dart';
export 'src/ui/shared/error_dialog.dart';
export 'src/ui/shared/outdated_content_dialog.dart';
export 'src/ui/shared/scanner_error_handler.dart';

// Portas de plataforma — a implementação vive na casca.
export 'src/domain/ports/document_date_extractor.dart';
export 'src/domain/ports/document_scanner_service.dart';
export 'src/domain/ports/file_picker_service.dart';
export 'src/domain/ports/file_save_service.dart';

// Domínio — famílias seladas de exceção
export 'src/domain/errors/batch_document_exception.dart';
export 'src/domain/errors/batch_download_exception.dart';
export 'src/domain/errors/department_exception.dart';
export 'src/domain/errors/document_dashboard_exception.dart';
export 'src/domain/errors/document_group_exception.dart';
export 'src/domain/errors/document_scanner_exception.dart';
export 'src/domain/errors/document_template_exception.dart';
export 'src/domain/errors/employee_exception.dart';
export 'src/domain/errors/require_document_exception.dart';
export 'src/domain/errors/workplace_exception.dart';

// Dados — implementações de repositório (a casca as instancia)
export 'src/data/repositories/batch_document_repository_impl.dart';
export 'src/data/repositories/batch_download_repository_impl.dart';
export 'src/data/repositories/cep_repository_impl.dart';
export 'src/data/repositories/company_repository_impl.dart';
export 'src/data/repositories/department_repository_impl.dart';
export 'src/data/repositories/document_dashboard_repository_impl.dart';
export 'src/data/repositories/document_group_repository_impl.dart';
export 'src/data/repositories/document_scanner_repository_impl.dart';
export 'src/data/repositories/document_template_repository_impl.dart';
export 'src/data/repositories/employee_repository_impl.dart';
export 'src/data/repositories/require_document_repository_impl.dart';
export 'src/data/repositories/workplace_repository_impl.dart';

// Dados — api services (a casca as instancia)
export 'src/data/services/batch_document_api_service.dart';
export 'src/data/services/batch_download_api_service.dart';
export 'src/data/services/cep_api_service.dart';
export 'src/data/services/company_api_service.dart';
export 'src/data/services/department_api_service.dart';
export 'src/data/services/document_dashboard_api_service.dart';
export 'src/data/services/document_group_api_service.dart';
export 'src/data/services/document_template_api_service.dart';
export 'src/data/services/employee_api_service.dart';
export 'src/data/services/http_status_helper.dart';
export 'src/data/services/multipart_upload_helper.dart';
export 'src/data/services/request_id_helper.dart';
export 'src/data/services/require_document_api_service.dart';
export 'src/data/services/spreadsheet_service.dart';
export 'src/data/services/workplace_api_service.dart';

// Utilitários do produto
export 'src/utils/combine_file_namer.dart';
export 'src/utils/concurrency.dart';
export 'src/utils/error_messages.dart';
export 'src/utils/fuzzy_name_matcher.dart';
export 'src/utils/image_to_pdf_converter.dart';
export 'src/utils/page_rotation_finder.dart';
export 'src/utils/pdf_merger.dart';
export 'src/utils/pdf_text_extractor.dart';
export 'src/utils/zip_builder.dart';

// DTO que NAO e detalhe interno: aparece na assinatura de
// EmployeeRepository, que e exportado. Os demais *_api_model ficam
// privados — quem esta fora usa entidade, nunca o shape da API.
export 'src/data/models/document_range_item.dart';
export 'src/domain/entities/document_content_status.dart';
export 'src/domain/entities/document_status_labels.dart';
export 'src/domain/errors/document_content_exception.dart';
export 'src/domain/repositories/document_content_repository.dart';
export 'src/data/models/document_content_status_api_model.dart';
export 'src/data/repositories/document_content_repository_impl.dart';
export 'src/data/services/document_content_api_service.dart';
export 'src/utils/page_splitter.dart';
export 'src/ui/batch_document/widgets/split_scan_dialog.dart';
