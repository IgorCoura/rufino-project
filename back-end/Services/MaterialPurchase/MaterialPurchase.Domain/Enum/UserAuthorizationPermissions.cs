﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Enum
{
    public enum UserAuthorizationPermissions
    {
        Client,
        Creator,
        Receiver,
        Supervisor,
        Admin
    }
}