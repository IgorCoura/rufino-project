using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Models.Construction
{
    public record CreateConstructionModel(
        string CorporateName, 
        string NickName, 
        string Street, 
        string City, 
        string State, 
        string Country, 
        string ZipCode
    );
}
