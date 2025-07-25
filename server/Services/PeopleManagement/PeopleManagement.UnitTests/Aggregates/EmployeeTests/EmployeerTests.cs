﻿using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using System.Text.Json;

namespace PeopleManagement.UnitTests.Aggregates.EmployeeTests
{
    public class EmployeerTests
    {
        private static Employee GetValidEmployee => Employee.Create(Guid.NewGuid(), Guid.NewGuid(), "Ritinha Valvense");

        [Theory]
        [InlineData("Rogerio Junior")]
        public void CreateValidEmployeer(string name)
        {
            var id = Guid.NewGuid();
            var companyId = Guid.NewGuid();

            var employeer = Employee.Create(id, companyId, name);


            Assert.Equal(id, employeer.Id);
            Assert.Equal(companyId, employeer.CompanyId);
            Assert.Equal(name, employeer.Name.ToString(), StringComparer.OrdinalIgnoreCase);
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

            Assert.Equal(email, employee.Contact.Email);
            Assert.Equal(phone, employee.Contact.CellPhone);
        }

        [Fact]
        public void AddValidDepedentToEmployer()
        {
            var employee = GetValidEmployee;

            var name = "Maria Atunieta";

            var dependent = Dependent.Create(
                name,
                IdCard.Create("216.456.330-12", "Maria Silva", "Marcio Andrade", "Suzano", "São Paulo", "Brasileiro", DateOnly.Parse("2000/01/01")),
                Gender.FEMALE,
                DependencyType.Spouse
                );

            employee.AddDependent(dependent);

            Assert.Contains(name, employee.Dependents.Select(x => x.Name.ToString()), StringComparer.OrdinalIgnoreCase);
        }



        [Fact]
        public void CompleteEmployeeAdmissionWithSuccess()
        {
            var employee = GetValidEmployee;

            employee.RoleId = Guid.NewGuid();
            employee.WorkPlaceId = Guid.NewGuid();

            employee.Address = Address.Create("69015-756", "Rua 11", "936", "", "Colônia Terra Nova", "Manaus", "Amazonia", "Brasil");
            employee.Contact = Contact.Create("email@email.com", "(00) 100000001");
            employee.PersonalInfo = PersonalInfo.Create(Deficiency.Create("", []), MaritalStatus.Single, Gender.MALE, Ethinicity.White, EducationLevel.CompleteHigher);
            employee.IdCard = IdCard.Create("216.456.330-12", "Maria Silva", "Marcio Andrade", "Suzano", "São Paulo", "Brasileiro", DateOnly.Parse("2000/01/01"));
            employee.VoteId = VoteId.Create("281662310124");

            employee.MedicalAdmissionExam = MedicalAdmissionExam.Create(DateOnly.FromDateTime(DateTime.Now), DateOnly.FromDateTime(DateTime.Now.AddDays(365)));

            employee.MilitaryDocument = MilitaryDocument.Create("2312312312", "Rersevista");

            var dateNow = DateOnly.FromDateTime(DateTime.UtcNow);

            employee.CompleteAdmission("BBC2", dateNow,  EmploymentContractType.CLT);

            var json = JsonSerializer.Serialize(employee);


            Assert.True(employee.Status == Status.Active);
        }

    }
}
