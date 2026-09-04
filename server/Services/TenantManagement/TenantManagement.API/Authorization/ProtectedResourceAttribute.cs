namespace TenantManagement.API.Authorization;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class ProtectedResourceAttribute : AuthorizeAttribute
{
    public const string POLICY_PREFIX = "ProtectedResource:";

    public ProtectedResourceAttribute(string resource)
        :this (resource, [])
    {
        
    }

    public ProtectedResourceAttribute(string resource, string scope)
        : this(resource, [scope])
    {

    }

    public ProtectedResourceAttribute(string resource, string[] scopes)
    {
        this.Resource = resource;
        this.Scopes = scopes;
        this.Policy = GetPolicy(resource, scopes);
    }

    public string Resource { get; init; }
    public string[] Scopes { get; init; }
    public string GetScopesExpression() => string.Join(',', this.Scopes.Where(s => !string.IsNullOrWhiteSpace(s)));
    public static string GetScopesExpression(string[] scopes) => string.Join(',', scopes.Where(s => !string.IsNullOrWhiteSpace(s)));
    private static string GetPolicy(string resource, string[] scopes)
    {
        return $"{POLICY_PREFIX}{resource}#{GetScopesExpression(scopes)}";
    }
}
