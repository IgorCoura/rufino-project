
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.CompanyAggregate
{
    public class CNPJ : ValueObject
    {
        public const int MAX_LENGTH = 14;
        private string _value = string.Empty;

        public string Value 
        { 
            get => _value;
            private set
            {
                if (!ValidityCNPJ(value))
                    throw new DomainException(this, DomainErrors.FieldInvalid(nameof(CNPJ), value));
                _value = value;
            }
        }

        public CNPJ(string value)
        {
            Value = value;
        }

        public static implicit operator CNPJ(string value) => new(value);

        /// <summary>
        /// Informar um CNPJ completo para validação do digito verificador
        /// </summary>
        /// <param name="cnpj">string com o numero CNPJ completo com Digito</param>
        /// <returns>Boolean True/False onde True=Digito CNPJ Valido</returns>
        private static Boolean ValidityCNPJ(string cnpj)
        {

            // Retira carcteres invalidos não numericos da string
            var cnpjChars = cnpj.Select(x => char.IsDigit(x) ? x : ' ').ToArray();
            var new_cnpj = new string(cnpjChars).Replace(" ", "");

            if (string.IsNullOrEmpty(new_cnpj))
                return false;


            // Verifica se o CNPJ informado tem os 14 digitos 
            if (new_cnpj.Length != MAX_LENGTH)
            {
                return false;
            }

            // Calcula o digito do CNPJ e compara com o digito informado
            var digitoVerificador = CalculaDigCNPJ(new_cnpj);
            if (digitoVerificador == new_cnpj.Substring(12, 2))
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// Calcula o Digito verificador de um CNPJ informado  
        /// </summary>
        /// <param name="cnpj">string com o CNPJ contendo 12 digitos e sem o digito verificador</param>
        /// <returns>string com o digito calculado do CNPJ ou null caso o CNPJ informado for maior que 12 digitos</returns>
        private static string CalculaDigCNPJ(string cnpj)
        {
            cnpj = cnpj[..12];

            // Declara variaveis para uso
            string digito = "";
            int[] calculo = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

            // Calcula o primeiro digito do CNPJ
            int aux1 = 0;

            for (int i = 0; i < cnpj.Length; i++)
            {
                aux1 += Convert.ToInt32(cnpj.Substring(i, 1)) * calculo[i];
            }

            int aux2 = aux1 % 11;

            // Carrega o primeiro digito na variavel digito
            if (aux2 < 2)
            {
                digito += "0";
            }
            else
            {
                digito += (11 - aux2).ToString();
            }

            // Adiciona o primeiro digito ao final do CNPJ para calculo do segundo digito
            cnpj += digito;

            // Calcula o segundo digito do CNPJ
            calculo = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
            aux1 = 0;

            for (int i = 0; i < cnpj.Length; i++)
            {
                aux1 += Convert.ToInt32(cnpj.Substring(i, 1)) * calculo[i];
            }

            aux2 = (aux1 % 11);

            // Carrega o segundo digito na variavel digito
            if (aux2 < 2)
            {
                digito += "0";
            }
            else
            {
                digito += (11 - aux2).ToString();
            }

            return digito;
        }


        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return _value;
        }
    }
}
