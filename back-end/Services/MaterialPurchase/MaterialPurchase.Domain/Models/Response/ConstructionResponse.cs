﻿using MaterialPurchase.Domain.Entities;


namespace MaterialPurchase.Domain.Models.Response
{
    public record ConstructionResponse
    (
        Guid Id,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        string CorporateName,
        string NickName,
        Address? Address
    );
}
