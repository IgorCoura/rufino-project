using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConversorMedicoesProjetos
{
    public class Medicao
    {
        public Medicao(string chave,  string descricao, string unid, string quant)
        {
            Chave = chave;
            Descricao = descricao;
            Unid = unid;
            Quant = quant;
        }

        private string chave;
        private string categoria;
        private string descricao;
        private string unid;
        private string quant;

        public string Chave 
        { 
            get => chave;
            set => chave = value.Trim();
        }
        public string Categoria 
        {
            get => categoria;
            set => categoria = value.Trim();
        }
        public string Descricao 
        {
            get => descricao;
            set => descricao = value.Trim();
        }
        public string Unid 
        {
            get => unid;
            set => unid = value.Trim();
        }
        public string Quant 
        {
            get => quant;
            set => quant = value.Trim().Replace(".",",");
        }


        public void SetCategoria(string[] allLinesAuxTab)
        {
            foreach (string line in allLinesAuxTab)
            {
                var items = line.Split(';');

                var chave  = items[0];
                var categoria = items[1];

                if(chave.Trim() == Chave)
                {
                    Categoria = categoria;
                    return;
                }
            }
            
        }

        public string ToCsv()
        {
            return $"{Chave};{Categoria};{Descricao};{Unid};{Quant};";
        }
    }
}
