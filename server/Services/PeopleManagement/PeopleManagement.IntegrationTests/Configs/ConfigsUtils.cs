using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;

namespace PeopleManagement.IntegrationTests.Configs
{
    public static class ConfigsUtils
    {
        public static HttpClient InputHeaders(this HttpClient httpClient, Guid[]? companies = default, string authorization = "", string xRequestId = "")
        {
            authorization = string.IsNullOrWhiteSpace(authorization) ? Guid.NewGuid().ToString() : authorization;
            xRequestId = string.IsNullOrWhiteSpace(xRequestId) ? Guid.NewGuid().ToString() : xRequestId;
            companies ??= [];

            // Remove antes de adicionar: DefaultRequestHeaders.Add lança se o header já existe, então isso torna
            // o método idempotente (seguro chamar mais de uma vez no mesmo HttpClient).
            httpClient.DefaultRequestHeaders.Remove("x-requestid");
            httpClient.DefaultRequestHeaders.Remove("Authorization");
            httpClient.DefaultRequestHeaders.Remove("companies");

            // Usa os parâmetros recebidos (antes eram calculados e descartados por um Guid novo, impedindo
            // fixar um x-requestid determinístico para testar idempotência).
            httpClient.DefaultRequestHeaders.Add("x-requestid", xRequestId);
            httpClient.DefaultRequestHeaders.Add("Authorization", authorization);

            // Só envia "companies" quando há ids: um valor vazio viraria [""] no Split(',') do
            // MockAccessRequirementHandler. Quando presente, vai como um único valor separado por vírgula.
            if (companies.Length > 0)
                httpClient.DefaultRequestHeaders.Add("companies", string.Join(",", companies.Select(x => x.ToString())));

            return httpClient;
        }
    }
}
