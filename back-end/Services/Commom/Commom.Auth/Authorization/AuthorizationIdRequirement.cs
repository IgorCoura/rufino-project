﻿using Microsoft.AspNetCore.Authorization;

namespace Commom.Auth.Authorization
{
    public class AuthorizationIdRequirement : IAuthorizationRequirement
    {
        public AuthorizationIdRequirement (string id) =>
        Id =  id;

        public string Id  { get; }
    }
}