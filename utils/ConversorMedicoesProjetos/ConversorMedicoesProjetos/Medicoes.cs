using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConversorMedicoesProjetos
{
    public class Medicoes
    {
        public Medicoes(string title)
        {
            Title = title;
        }

        public string Title { get; set; }
        public List<Medicao> ListMedicoes { get; set; } = [];


        public void Add(Medicao medicao)
        {
            ListMedicoes.Add(medicao);
        }

        public List<string> ToCSV()
        {
            var list = new List<string>();
            list.Add($";;{Title};;;");
            list.AddRange(ListMedicoes.Select(x => x.ToCsv()).ToList());
            return list;
        }
    }
}
