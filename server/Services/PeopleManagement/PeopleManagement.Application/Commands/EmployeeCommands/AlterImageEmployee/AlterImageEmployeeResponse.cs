using PeopleManagement.Application.Commands.EmployeeCommands.AlterMedicalAdmissionExamEmployee;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterImageEmployee
{
    public record AlterImageEmployeeResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator AlterImageEmployeeResponse(Guid id) => new(id);
    }
}
