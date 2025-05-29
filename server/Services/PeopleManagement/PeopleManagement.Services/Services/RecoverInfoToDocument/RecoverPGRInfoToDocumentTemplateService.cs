using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using System.Text.Json.Nodes;
using PeopleManagement.Domain.AggregatesModel.DepartmentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DepartmentAggregate;
using PeopleManagement.Domain.AggregatesModel.PositionAggregate;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;

namespace PeopleManagement.Services.Services.RecoverInfoToDocument
{
    public class RecoverPGRInfoToDocumentTemplateService(IDepartmentRepository departmentRepository) : IRecoverPGRInfoToDocumentTemplateService
    {
        private readonly IDepartmentRepository _departmentRepository = departmentRepository;
        public async Task<JsonObject> RecoverInfo(Guid employeeId, Guid companyId, CancellationToken cancellation = default)
        {
            var department = await _departmentRepository.GetDepartmentFromEmployeeId(employeeId, companyId, cancellation)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Department), employeeId.ToString()));

            var result = new JsonObject
            {
                ["PGR"] = new JsonObject
                    {
                        ["risks"] = GetRisks(department.Name.Value),
                        ["epis"] = GetEpis(department.Name.Value),
                    }
            };

            return result;
        }
        private static JsonObject riskRadiacao => new JsonObject
        {
            ["name"] = "Radiação não ionizante",
            ["source"] = "Trabalho a céu aberto",
            ["precautions"] = "Existência de abrigos, capazes de proteger os trabalhadores contra intempéries climáticos",
        };

        private static JsonObject  riskRuido => new JsonObject
        {
            ["name"] = "Ruído",
            ["source"] = "Máquinas da Obra",
            ["precautions"] = "Treinamento de integração, fornecimento de EPI",
        };

        private static JsonObject riskPoeirasMinerias => new JsonObject
        {
            ["name"] = "Poeiras Minerias",
            ["source"] = "Ambiente de Obra",
            ["precautions"] = "Procedimentos, orientações e treinamento para a execução do trabalho",
        };

        private static JsonObject riskCargas => new JsonObject
        {
            ["name"] = "Levantamento e transporte manual de cargas e volumes",
            ["source"] = "Movimentação de materias",
            ["precautions"] = "Ginástica Laboral, treinamento Ergonômico e DDS",
        };

        private static JsonObject riskPe => new JsonObject
        {
            ["name"] = "Postura de pé por longos períodos",
            ["source"] = "Processos de Trabalho",
            ["precautions"] = "Ginástica Laboral, treinamento Ergonômico e DDS",
        };

        private static JsonObject riskPostura => new JsonObject
        {
            ["name"] = "Postura inadequada",
            ["source"] = "Processos de Trabalho",
            ["precautions"] = "Ginástica Laboral, treinamento Ergonômico e DDS",
        };

        private static JsonObject riskBater => new JsonObject
        {
            ["name"] = "Bater Contra",
            ["source"] = "Ambiente de Obra, Mobiliário e Equipamentos",
            ["precautions"] = "Manter o ambiente de trabalho organizado, evitando colocar objetos e mobiliário nas áreas de circulação de pessoas",
        };

        private static JsonObject riskChoque => new JsonObject
        {
            ["name"] = "Choque elétrico",
            ["source"] = "Processos de Trabalho",
            ["precautions"] = "Procedimentos, orientações, treinamento e competência para a execução do trabalho. Fornecimento de EPI",
        };

        private static JsonObject riskProjecao => new JsonObject
        {
            ["name"] = "Projeção de partículas",
            ["source"] = "Ambiente de Obra",
            ["precautions"] = "Procedimentos, orientações, treinamento e competência para a execução do trabalho, fornecimento de EPI",
        };

        private static JsonObject riskQueda => new JsonObject
        {
            ["name"] = "Queda de diferença de nível maior que dois metros",
            ["source"] = "Trabalho realizado emaltura",
            ["precautions"] = "Treinamento de Trabalho emAltura NR-35 e fornecimento de EPIs. Realização de exames específicos conforme o PCMSO",
        };
        private static JsonArray GetRisks(string department)
        {
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

            var risks = new Dictionary<string, dynamic>
            {
                { "ELETRICA", risksELetrica },
                { "HIDRAULICA", risksHidraulica },
                { "ADMINISTRAÇÃO DE OBRA", risksAdm }
            };

            try
            {
                return risks[department.ToUpper()];
            }
            catch
            {
                return new JsonArray();
            }

        }

        private static dynamic GetEpis(string department)
        {
            var epis = new Dictionary<string, dynamic>
            {
                {
                    "ELETRICA",
                    new JsonArray()
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
                },
                {
                    "HIDRAULICA",
                    new JsonArray()
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
                },
                {
                    "ADMINISTRAÇÃO DE OBRA",
                    new JsonArray()
                {
                    "Protetor auricular tipo plug de inserção",
                    "Máscara PFF2",
                    "Luva Tátil",
                    "Calçado de Segurança",
                    "Capacete de segurança"
                }
                }
            };

            try
            {
                return epis[department.ToUpper()];
            }
            catch
            {
                return new JsonArray();
            }
        }
    }
}
