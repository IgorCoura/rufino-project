namespace PeopleManagement.IntegrationTests.Configs
{
    public static class ConfigsUtils
    {
        /// <summary>
        /// Nome do header que a suíte usa para declarar os tenants da pessoa — e que TEM que ser o
        /// mesmo claim que a produção lê (<c>Keycloak:RouteClaimTypeRequirement</c>).
        /// </summary>
        /// <remarks>
        /// Era <c>companies</c>, o claim legado que nenhum código produz desde 2026-09-03. A suíte
        /// ficava verde exercitando um guard que a produção não monta: trocar o claim no
        /// <c>appsettings</c> não quebrava teste nenhum. Quem impede a divergência voltar é
        /// <c>RouteGuardTests.OGuardDeRota_TemQueLerOMesmoClaimQueASuiteEnvia</c>.
        /// </remarks>
        public const string TENANT_CLAIM_HEADER = "pm_tenants";

        public static HttpClient InputHeaders(this HttpClient httpClient, Guid[]? companies = default, string authorization = "", string xRequestId = "")
        {
            ArgumentNullException.ThrowIfNull(httpClient);

            authorization = string.IsNullOrWhiteSpace(authorization) ? Guid.NewGuid().ToString() : authorization;
            xRequestId = string.IsNullOrWhiteSpace(xRequestId) ? Guid.NewGuid().ToString() : xRequestId;
            companies ??= [];

            // Remove antes de adicionar: DefaultRequestHeaders.Add lança se o header já existe, então isso torna
            // o método idempotente (seguro chamar mais de uma vez no mesmo HttpClient).
            httpClient.DefaultRequestHeaders.Remove("x-requestid");
            httpClient.DefaultRequestHeaders.Remove("Authorization");
            httpClient.DefaultRequestHeaders.Remove(TENANT_CLAIM_HEADER);

            // Usa os parâmetros recebidos (antes eram calculados e descartados por um Guid novo, impedindo
            // fixar um x-requestid determinístico para testar idempotência).
            httpClient.DefaultRequestHeaders.Add("x-requestid", xRequestId);
            httpClient.DefaultRequestHeaders.Add("Authorization", authorization);

            // Só envia o claim quando há ids: um valor vazio viraria [""] no Split(',') do
            // MockAccessRequirementHandler. Quando presente, vai como um único valor separado por vírgula.
            if (companies.Length > 0)
                httpClient.DefaultRequestHeaders.Add(TENANT_CLAIM_HEADER, string.Join(",", companies.Select(x => x.ToString())));

            return httpClient;
        }
    }
}
