using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeopleManagement.Domain.Exceptions
{
    public static class PeopleManagementErrors
    {
        public static ErrorModel InvalidFieldError(string nameField, string valor) => new ("10", $"O campo {nameField} com o valor {valor} é invalido.");

    }
}
