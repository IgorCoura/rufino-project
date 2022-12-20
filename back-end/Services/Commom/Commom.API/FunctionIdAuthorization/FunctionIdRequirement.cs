using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.API.FunctionIdAuthorization
{
    public class FunctionIdRequirement : IAuthorizationRequirement
    {
        public FunctionIdRequirement (string id) =>
        Id =  id;

        public string Id  { get; }
    }
}
