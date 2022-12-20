using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.API.FunctionIdAuthorization
{
    public class FunctionIdAuthorizeAttribute : AuthorizeAttribute
    {
        const string POLICY_PREFIX = "FunctionId";

        public FunctionIdAuthorizeAttribute(string id) => Id = id;

        public string Id 
        {
            get
            {
                return Policy[POLICY_PREFIX.Length..];
            }
            set
            {
                Policy = $"{POLICY_PREFIX}{value}";
            }
        }
    }
}
