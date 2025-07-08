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

            httpClient.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            httpClient.DefaultRequestHeaders.Add("Authorization", Guid.NewGuid().ToString());
            httpClient.DefaultRequestHeaders.Add("companies", companies.Select(x => x.ToString()));

            return httpClient;
        }
    }
}
