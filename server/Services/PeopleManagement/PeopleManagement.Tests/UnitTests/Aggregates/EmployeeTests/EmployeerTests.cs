using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Tests.UnitTests.Aggregates.EmployeeTests
{
    public class EmployeerTests
    {
        private static  Employee GetValidEmployee => Employee.Create(Guid.NewGuid(), Guid.NewGuid(), "Ritinha Valvense");

        [Theory]
        [InlineData("Rogerio Junior")]
        public void CreateValidEmployeer(string name)
        {
            var id = Guid.NewGuid();
            var companyId = Guid.NewGuid();

            var employeer = Employee.Create(id, companyId, name);


            Assert.Equal(id, employeer.Id);
            Assert.Equal(companyId, employeer.CompanyId);
            Assert.Equal(name, employeer.Name);
        }
        
        [Fact]
        public void AddValidRoleIdToEmployer()
        {
            var id = Guid.NewGuid();
            var employee = GetValidEmployee;

            employee.RoleId = id;


            Assert.Equal(id, employee.RoleId);
        }

        [Fact]
        public void AddValidWorkPlaceToEmployer()
        {
            var id = Guid.NewGuid();
            var employee = GetValidEmployee;

            employee.WorkPlaceId = id;

            Assert.Equal(id, employee.WorkPlaceId);
        }

        [Theory]
        [InlineData("email@email.com", "11987483929")]
        public void AddValidContactToEmployer(string email, string phone)
        {
            var employee = GetValidEmployee;

            employee.Contact = Contact.Create(email, phone);

            Assert.Equal(email , employee.Contact.Email);
            Assert.Equal(phone , employee.Contact.CellPhone);
        }

        [Fact]
        public void AddValidDepedentToEmployer()
        {
            var employee = GetValidEmployee;
            var id = Guid.NewGuid();

            var dependent = Dependent.Create(
                id,
                "Maria Atunieta",
                IdCard.Create("12312455", "216.456.330-12", DateOnly.Parse("2015-12-12"), "SSP/SP", "Maria Silva", "Marcio Andrade", "Suzano", "São Paulo", "Brasileiro", DateOnly.Parse("2000/01/01")),
                Gender.FEMALE,
                DependencyType.Spouse
                );

            employee.AddDependent(dependent);


            Assert.Collection(employee.Dependents, x => Assert.Equal(id, x.Id));
        }



        [Fact]
        public void CompleteEmployeeAdmissionWithSuccess()
        {
            var employee = GetValidEmployee;

            employee.RoleId = Guid.NewGuid();
            employee.WorkPlaceId = Guid.NewGuid();

            employee.Address = Address.Create("69015-756", "Rua 11", "936", "", "Colônia Terra Nova", "Manaus", "Amazonia", "Brasil");

            employee.Sip = "873.58571.07-9";

            employee.PersonalInfo = PersonalInfo.Create(Deficiency.Create("", []), MaritalStatus.Single, Gender.MALE, Ethinicity.White, EducationLevel.CompleteHigher);
            employee.IdCard = IdCard.Create("12312455", "216.456.330-12", DateOnly.Parse("2015-12-12"), "SSP/SP", "Maria Silva", "Marcio Andrade", "Suzano", "São Paulo", "Brasileiro", DateOnly.Parse("2000/01/01"));
            employee.VoteId = VoteId.Create("281662310124");

            employee.AddMedicalExam(MedicalExam.Create(DateOnly.Parse("2024/04/20"), DateOnly.Parse("2025/04/20")));

            employee.MilitaryDocument = MilitaryDocument.Create("2312312312", "Rersevista");

            employee.CompleteAdmission("BBC2", EmploymentContactType.CLT);


            Assert.True(employee.Status == Status.Active);
        }

    }
}
