using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.PositionAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DepartmentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate;
using PeopleManagement.Domain.AggregatesModel.PositionAggregate;
using PeopleManagement.Domain.AggregatesModel.DepartmentAggregate;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using System.Json;

namespace PeopleManagement.Services.Services.RecoverInfoToSecurityDocument
{
    public class RecoverNR01InfoToSecurityDocumentService : IRecoverNR01InfoToSecurityDocumentService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IPositionRepository _positionRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly ICompanyRepository _companyRepositoty;

        public RecoverNR01InfoToSecurityDocumentService(IEmployeeRepository employeeRepository, IRoleRepository roleRepository, IPositionRepository positionRepository, IDepartmentRepository departmentRepository, ICompanyRepository companyRepositoty)
        {
            _employeeRepository = employeeRepository;
            _roleRepository = roleRepository;
            _positionRepository = positionRepository;
            _departmentRepository = departmentRepository;
            _companyRepositoty = companyRepositoty;
        }

        public async Task<string> RecoverInfo(Guid id, Guid companyId, DateTime date, CancellationToken cancellation = default)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellation: cancellation)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), id.ToString()));
            var role = await _roleRepository.FirstOrDefaultAsync(x => x.Id == employee.RoleId && x.CompanyId == companyId, cancellation: cancellation)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Role), employee.RoleId!.ToString()!));
            var position = await _positionRepository.FirstOrDefaultAsync(x => x.Id == role.PositionId && x.CompanyId == companyId, cancellation: cancellation)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Position), role.PositionId!.ToString()!));
            var department = await _departmentRepository.FirstOrDefaultAsync(x => x.Id == position.DepartmentId && x.CompanyId == companyId, cancellation: cancellation)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Department), position.DepartmentId!.ToString()!));
            var company = await _companyRepositoty.FirstOrDefaultAsync(x => x.Id == companyId, cancellation: cancellation)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Company), companyId.ToString()));

            var result = new JsonObject
            {
                ["name"] = $"{employee.Name}",
                ["registration"] = $"{employee.Registration}",
                ["cpf"] = $"{employee.IdCard!.Cpf}",
                ["department"] = $"{department.Name}",
                ["position"] = $"{position.Name}",
                ["role"] = $"{role.Name}",
                ["cbo"] = $"{role.CBO}",
                ["description"] = $"{role.Description}",
                ["company"] =$"{company.FantasyName}",
                ["risks"] = GetRisks(department.Name.Value),
                ["epis"] = GetEpis(department.Name.Value),
                ["date"] = $"{date}"
            };
            
            return result.ToString();
        }

        private JsonArray GetRisks(string department)
        {
            var riskRadiacao = new JsonObject 
            {
                ["name"] = "Radiação não ionizante",
                ["source"] = "Trabalho a céu aberto",
                ["precautions"] = "Existência de abrigos, capazes de proteger os trabalhadores contra intempéries climáticos",
            };

            var riskRuido = new JsonObject
            {
                ["name"] = "Ruído",
                ["source"] = "Máquinas da Obra",
                ["precautions"] = "Treinamento de integração, fornecimento de EPI",
            };

            var riskPoeirasMinerias = new JsonObject
            {
                ["name"] = "Poeiras Minerias",
                ["source"] = "Ambiente de Obra",
                ["precautions"] = "Procedimentos, orientações e treinamento para a execução do trabalho",
            };
            
            var riskCargas = new JsonObject
            {
                ["name"] = "Levantamento e transporte manual de cargas e volumes",
                ["source"] = "Movimentação de materias",
                ["precautions"] = "Ginástica Laboral, treinamento Ergonômico e DDS",
            };
            
            var riskPe = new JsonObject
            {
                ["name"] = "Postura de pé por longos períodos",
                ["source"] = "Processos de Trabalho",
                ["precautions"] = "Ginástica Laboral, treinamento Ergonômico e DDS",
            };
            
            var riskPostura = new JsonObject
            {
                ["name"] = "Postura inadequada",
                ["source"] = "Processos de Trabalho",
                ["precautions"] = "Ginástica Laboral, treinamento Ergonômico e DDS",
            };
            
            var riskBater = new JsonObject
            {
                ["name"] = "Bater Contra",
                ["source"] = "Ambiente de Obra, Mobiliário e Equipamentos",
                ["precautions"] = "Manter o ambiente de trabalho organizado, evitando colocar objetos e mobiliário nas áreas de circulação de pessoas",
            };
            
            var riskChoque = new JsonObject
            {
                ["name"] = "Choque elétrico",
                ["source"] = "Processos de Trabalho",
                ["precautions"] = "Procedimentos, orientações, treinamento e competência para a execução do trabalho. Fornecimento de EPI",
            };

            var riskProjecao = new JsonObject
            {
                ["name"] = "Projeção de partículas",
                ["source"] = "Ambiente de Obra",
                ["precautions"] = "Procedimentos, orientações, treinamento e competência para a execução do trabalho, fornecimento de EPI",
            }; 
            
            var riskQueda = new JsonObject
            {
                ["name"] = "Queda de diferença de nível maior que dois metros",
                ["source"] = "Trabalho realizado emaltura",
                ["precautions"] = "Treinamento de Trabalho emAltura NR-35 e fornecimento de EPIs. Realização de exames específicos conforme o PCMSO",
            };


            var risksELetrica = new JsonArray()
            {
                riskRadiacao, riskRuido, riskPoeirasMinerias, riskCargas, riskPe, riskPostura, riskBater, riskProjecao, riskQueda, riskChoque
            };

            var risksHidraulica = new JsonArray()
            {
                riskRadiacao, riskRuido, riskPoeirasMinerias, riskCargas, riskPe, riskPostura, riskBater, riskProjecao, riskQueda
            };

            var risksAdm = new JsonArray()
            {
                riskRuido, riskPoeirasMinerias, riskCargas, riskPostura, riskBater
            };

            var risks = new Dictionary<string, dynamic>();

            risks.Add("ELETRICA", risksELetrica);
            risks.Add("HIDRAULICA", risksHidraulica);
            risks.Add("ADMINISTRAÇÃO DE OBRA", risksAdm);

            return risks[department.ToUpper()];
        }

        private dynamic GetEpis(string department)
        {
            var epis = new Dictionary<string, dynamic>();

            epis.Add("ELETRICA", new JsonArray()
                {
                    "Protetor Solar",
                    "Protetor auricular tipo plug de inserção",
                    "Máscara PFF2",
                    "Luva Tátil",
                    "Calçado de Segurança",
                    "Óculos de proteção",
                    "Capacete de segurança",
                    "Cinturão de segurança com talabarte e trava-queda",
                    "Luva emborrachada"
                }
            );

            epis.Add("HIDRAULICA", new JsonArray()
                {
                    "Protetor Solar",
                    "Protetor auricular tipo plug de inserção",
                    "Máscara PFF2",
                    "Luva Tátil",
                    "Calçado de Segurança",
                    "Óculos de proteção",
                    "Capacete de segurança",
                    "Cinturão de segurança com talabarte e trava-queda"
                }
            );

            epis.Add("ADMINISTRAÇÃO DE OBRA", new JsonArray()
                {
                    "Protetor auricular tipo plug de inserção",
                    "Máscara PFF2",
                    "Luva Tátil",
                    "Calçado de Segurança",
                    "Capacete de segurança"
                }
            );

            return epis[department.ToUpper()];
        }
    }
}
