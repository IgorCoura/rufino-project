using System.Json;

namespace PeopleManagement.IntegrationTests.Data
{
    public static class DataToSecurityDocument
    {
        public static string GetContent()
        {
            var result = new JsonObject
            {
                ["name"] = $"ROSDEVALDO PEREIRA",
                ["registration"] = $"RU1234",
                ["cpf"] = $"21645633012",
                ["department"] = $"HIDRAULICA",
                ["position"] = $"ENCANADOR",
                ["role"] = $"ENCANADOR SENIOR",
                ["cbo"] = $"738298",
                ["description"] = $"ENCANADOR COM EXPERIENCIA",
                ["company"] = "RUFINO",
                ["risks"] = GetRisks(),
                ["epis"] = GetEpis(),
                ["riskstest"] = GetRisksTest(),
                ["date"] = $"{DateTime.Now.ToString("dddd,dd/mm/aaaa")}"
            };
            return result.ToString();
        }
        private static JsonArray GetRisks()
        {
            var riskRadiacao = new JsonObject
            {
                ["name"] = "Radiação não ionizante",
                ["content"] = new JsonObject
                {
                    ["source"] = "Trabalho a céu aberto",
                    ["precautions"] = "Existência de abrigos, capazes de proteger os trabalhadores contra intempéries climáticos",
                }
            };

            var riskRuido = new JsonObject
            {
                ["name"] = "Ruído",
                ["content"] = new JsonObject
                {            
                    ["source"] = "Máquinas da Obra",
                    ["precautions"] = "Treinamento de integração, fornecimento de EPI",
                }
            };

            var riskPoeirasMinerias = new JsonObject
            {
                ["name"] = "Poeiras Minerias",
                ["content"] = new JsonObject
                {
                    ["source"] = "Ambiente de Obra",
                    ["precautions"] = "Treinamento de integração, fornecimento de EPI",
                }
            };

            return new JsonArray()
            {
                riskRadiacao, riskPoeirasMinerias, riskRuido
            };
        }

        private static JsonArray GetRisksTest()
        {
            var riskRadiacao = new JsonObject
            {
                ["name"] = "Radiação não ionizante",
                ["riskscontent"] = new JsonArray
                {
                    new JsonObject { ["content"] = "Trabalho a céu aberto" },
                    new JsonObject { ["content"] = "Existência de abrigos, capazes de proteger os trabalhadores contra intempéries climáticos" }
                }
            };

            var riskRuido = new JsonObject
            {
                ["name"] = "Ruído",
                ["riskscontent"] = new JsonArray
                {
                    new JsonObject { ["content"] = "Máquinas da Obra" },
                    new JsonObject { ["content"] = "Treinamento de integração, fornecimento de EPI" }
                }
            };

            var riskPoeirasMinerias = new JsonObject
            {
                ["name"] = "Ruído",
                ["riskscontent"] = new JsonArray
                {
                    new JsonObject { ["content"] = "Ambiente de Obra" },
                    new JsonObject { ["content"] = "Treinamento de integração, fornecimento de EPI" }
                }
            };

            return new JsonArray()
            {
                riskRadiacao, riskPoeirasMinerias, riskRuido
            };
        }

        private static dynamic GetEpis()
        {
            var json = new JsonArray()
                {
                    new JsonObject { ["epi"] = "Protetor Solar" },
                    new JsonObject { ["epi"] = "Protetor auricular tipo plug de inserção" },
                    new JsonObject { ["epi"] = "Máscara PFF2" },
                    new JsonObject { ["epi"] = "Luva Tátil" },
                    new JsonObject { ["epi"] = "Calçado de Segurança" },
                    new JsonObject { ["epi"] = "Óculos de proteção" },
                    new JsonObject { ["epi"] = "Capacete de segurança" },
                    new JsonObject { ["epi"] = "Cinturão de segurança com talabarte e trava-queda" }
                };
            return json;
        }
    }
}
