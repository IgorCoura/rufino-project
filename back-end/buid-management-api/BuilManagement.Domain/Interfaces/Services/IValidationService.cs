using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Interfaces.Services
{
    public interface IValidationService
    {
        IEnumerable<ValidationResult> ValidateModel(object obj);
    }
}
