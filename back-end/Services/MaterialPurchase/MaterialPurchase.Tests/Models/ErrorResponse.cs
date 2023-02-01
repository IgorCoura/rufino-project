using Microsoft.AspNetCore.Mvc.Versioning;
using Xunit.Sdk;

namespace MaterialPurchase.Tests.Models
{
    public class ErrorResponse
    {
        public bool Success { get; set; }
        public List<ErrorDataResponse> Errors { get; set; } = new List<ErrorDataResponse>();
    }

    public class ErrorDataResponse
    {
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
