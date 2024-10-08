﻿using Commom.Domain.BaseEntities;
using MaterialPurchase.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Entities
{
    public class Purchase : Entity
    {
        public Provider? Provider { get; set; }
        public Guid ProviderId { get; set; }
        public Construction? Construction { get; set; }
        public Guid ConstructionId { get; set; }
        public virtual IEnumerable<ItemMaterialPurchase> Materials { get; set; } = new List<ItemMaterialPurchase>();
        public decimal Freight { get; set; } = 0;
        public IEnumerable<PurchaseAuthUserGroup> AuthorizationUserGroups { get; set; } = new List<PurchaseAuthUserGroup>();
        public PurchaseStatus Status { get; set; } = PurchaseStatus.Open;
        public DateTime? LimitDeliveryDate { get; set; }
        public IEnumerable<PurchaseDeliveryItem> PurchaseDeliveries { get; set; } = new List<PurchaseDeliveryItem>();
    }
}
