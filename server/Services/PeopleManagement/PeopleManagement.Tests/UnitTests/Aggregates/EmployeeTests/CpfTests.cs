using Newtonsoft.Json.Linq;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using System.Numerics;

namespace PeopleManagement.Tests.UnitTests.Aggregates.EmployeeTests
{
    public class CpfTests
    {
        [Theory]
        [InlineData("216.456.330-12")]
        [InlineData("988.205.810-86")]
        [InlineData("367.344.280-52")]
        [InlineData("36734428052")]
        [InlineData("36.734.42  80-52  ")]
        public void CreateValidCpf(string value)
        {
            //Act
            CPF cpf = value;
            //Assert
            Assert.Equal(value, cpf);
        }

        [Theory]
        [InlineData("216.456.330-22")]
        [InlineData("988.205.810-82")]
        [InlineData("367.344.280-53")]
        [InlineData("36734428059")]
        [InlineData("36.734.42  80-56  ")]
        public void CreateInvalidCpf(string value)
        {
            DomainException ex = Assert.Throws<DomainException>(() =>
            {
                CPF cpf = value;
                return cpf;
            });

            Assert.Equal(ex.Errors[nameof(CPF)].First(), DomainErrors.FieldInvalid(nameof(CPF), value));
        }

    }
}
