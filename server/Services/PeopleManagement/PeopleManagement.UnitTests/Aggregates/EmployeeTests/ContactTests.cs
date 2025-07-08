using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.SeedWord;

namespace PeopleManagement.UnitTests.Aggregates.EmployeeTests
{
    public class ContactTests
    {
        [Theory]
        [InlineData("email@email.com", "(11) 99-3345639")]
        [InlineData("irg-asd.email@email.com.br", "11456784379")]
        [InlineData("123hh$2irg-asd.email@email.com.br.ar", " 11()456  7843 -79   ")]
        public void CreateContactWithValidEmailAndPhone(string email, string phone)
        {
            var expectPhone = phone.ToUpper().Trim();
            expectPhone = expectPhone.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "");

            //Act

            var contact = Contact.Create(email, phone);

            //Assert
            Assert.Equal(expectPhone, contact.CellPhone);
            Assert.Equal(email, contact.Email);
        }

        [Theory]
        [InlineData("emai", "(11) 99-3345639")]
        [InlineData("emai@", "11993345639")]
        [InlineData("@emai", "11993345639")]
        [InlineData("@emai.com", "11993345639")]
        [InlineData("@.com", "11993345639")]
        public void CreateContactWithInalidEmail(string email, string phone)
        {
            DomainException ex = Assert.Throws<DomainException>(() => Contact.Create(email, phone));
            Assert.Equal(ex.Errors[nameof(Contact)].FirstOrDefault(), DomainErrors.FieldIsFormatInvalid(nameof(Contact.Email)));
        }

        [Theory]
        [InlineData("emai", "(11) 99-3345639")]
        [InlineData("emai@", "11993345639")]
        [InlineData("@emai", "11993345639")]
        [InlineData("@emai.com", "11993345639")]
        [InlineData("@.com", "11993345639")]
        public void CreateContactWithInalidEmailWithoutThrow(string email, string phone)
        {
            var result = ValueObject.CreateCatchingException(() => Contact.Create(email, phone));
            Assert.True(result.IsFailure);
            Assert.Equal(result.Errors[nameof(Contact)].FirstOrDefault(), DomainErrors.FieldIsFormatInvalid(nameof(Contact.Email)));
        }

    }

}
