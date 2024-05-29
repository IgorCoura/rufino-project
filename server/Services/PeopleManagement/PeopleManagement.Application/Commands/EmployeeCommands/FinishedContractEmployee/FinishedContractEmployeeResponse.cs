using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeopleManagement.Application.Commands.EmployeeCommands.FinishedContractEmployee
{
    public record FinishedContractEmployeeResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator FinishedContractEmployeeResponse(Guid id) => new(id);
    }
}
