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

// Utilitários de domínio
export 'src/utils/fuzzy_name_matcher.dart';

// Dados — DTOs.
//
// ANDAIME: pelo critério do `bill_payment` o mapper de DTO NÃO é API pública —
// quem está fora usa entidade, nunca o shape da API. Eles estão aqui só porque
// os testes de DTO ainda vivem na casca; saem do barril quando esses testes
// migrarem para o pacote (fase 10 do plano).
export 'src/data/models/batch_document_unit_api_model.dart';
export 'src/data/models/batch_download_api_model.dart';
export 'src/data/models/cep_lookup_model.dart';
export 'src/data/models/company_api_model.dart';
export 'src/data/models/department_api_model.dart';
export 'src/data/models/document_dashboard_api_model.dart';
export 'src/data/models/document_group_api_model.dart';
export 'src/data/models/document_group_with_documents_api_model.dart';
export 'src/data/models/document_group_with_templates_api_model.dart';
export 'src/data/models/document_range_item.dart';
export 'src/data/models/document_template_api_model.dart';
export 'src/data/models/employee_address_api_model.dart';
export 'src/data/models/employee_api_model.dart';
export 'src/data/models/employee_contact_api_model.dart';
export 'src/data/models/employee_contract_api_model.dart';
export 'src/data/models/employee_dependent_api_model.dart';
export 'src/data/models/employee_document_api_model.dart';
export 'src/data/models/employee_id_card_api_model.dart';
export 'src/data/models/employee_medical_exam_api_model.dart';
export 'src/data/models/employee_military_document_api_model.dart';
export 'src/data/models/employee_personal_info_api_model.dart';
export 'src/data/models/employee_profile_api_model.dart';
export 'src/data/models/employee_social_integration_program_api_model.dart';
export 'src/data/models/employee_vote_id_api_model.dart';
export 'src/data/models/period_api_model.dart';
export 'src/data/models/require_document_api_model.dart';
export 'src/data/models/workplace_api_model.dart';
