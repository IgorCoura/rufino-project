﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControl.Domain.Models.Response
{
    public record UnityResponse
    (
        Guid Id,
        string Name
    );
}
