using Commom.Domain.SeedWork;
using MaterialPurchase.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Consts
{
    public static class MaterialPurchaseAuthorizationId
    {
        public const string CreatePurchase = "1001";
        public const string UpdatePurchase = "1002";
        public const string DeletePurchase = "1003";
        public const string SendPurchase = "1004";
        public const string AuthorizePurchase = "1005";
        public const string UnlockPurchase = "1006";
        public const string ConfirmDeliveryDatePurchase = "1007";
        public const string ReceiveDeliveryPurchase = "1008";
        public const string CancelPurchaseCreator = "1009";
        public const string CancelPurchaseClient = "1010";
        public const string CancelPurchaseAdmin = "1011";
    }
}

