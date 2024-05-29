using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeopleManagement.Application.Commands.EmployeeCommands.FinishedContractEmployee
{
    public record FinishedContractEmployeeCommand(Guid EmployeeId, Guid CompanyId, DateOnly FinishDateContract) : IRequest<FinishedContractEmployeeResponse>
    {
    }
}
