﻿using MaterialPurchase.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Models.Response
{
    public record ItemMaterialPurchaseResponse
    (
        Guid Id,
        MaterialResponse? Material,
        decimal UnitPrice,
        BrandResponse? Brand,
        double Quantity
    );
}
