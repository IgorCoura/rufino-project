using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeopleManagement.Application.Commands.DTO
{
    public record BaseDTO
    {
        public Guid Id { get; }

        public BaseDTO(Guid id)
        {
            Id = id;
        }
    }
}
