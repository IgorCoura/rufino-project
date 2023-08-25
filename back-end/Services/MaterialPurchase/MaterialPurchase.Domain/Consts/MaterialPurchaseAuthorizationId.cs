using Commom.Domain.BaseEntities;
using MaterialPurchase.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Consts
{
    public class MaterialPurchaseAuthorizationId
    {
        public const string AdminPurchase = "1000";
        public const string CreatePurchase = "1001";
        public const string UpdatePurchase = "1002";
        public const string DeletePurchase = "1003";
        public const string SendPurchase = "1004";
        public const string AuthorizePurchase = "1005";
        public const string UnlockPurchase = "1006";
        public const string ConfirmDeliveryDatePurchase = "1007";
        public const string ReceiveDeliveryPurchase = "1008";
        public const string CancelPurchaseBeforeAuthoriz = "1009";
        public const string CancelPurchaseDuringAuthorize = "1010";
        public const string CancelPurchaseAfterAuthorize = "1011";
        public const string GetPurchaseSimple = "1012";
        public const string GetAllPurchaseSimple = "1013";
        public const string GetPurchaseWithMaterial = "1014";
        public const string GetAllPurchaseWithMaterial = "1015";
        public const string GetPurchaseComplete = "1016";
        public const string GetAllPurchaseComplete = "1017";

        public Dictionary<string, string> GetConstantes()
        {

            Type tipo = this.GetType();

            // Obter todos os campos da classe atual
            FieldInfo[] campos = tipo.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            // Criar um dicionário vazio para armazenar as constantes
            Dictionary<string, string> constantes = new();

            // Percorrer todos os campos da classe atual
            foreach (FieldInfo campo in campos)
            {
                // Verificar se o campo é uma constante
                if (campo.IsLiteral && !campo.IsInitOnly)
                {
                    // Obter o nome e o valor da constante
                    string nome = campo.Name;
                    string valor = campo.GetValue(null)!.ToString() ?? "";

                    // Adicionar a constante ao dicionário
                    constantes.Add(nome, valor);
                }
            }

            // Retornar o dicionário com as constantes
            return constantes;
        }
    }
}

