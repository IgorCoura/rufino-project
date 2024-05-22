using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate;
using PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate;
using Address = PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Address;
using Contact = PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Contact;
using Name = PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Name;

namespace PeopleManagement.Infra.Mapping
{
    public class EmployeeMap : EntityMap<Employee>
    {
        public override void Configure(EntityTypeBuilder<Employee> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Registration)             
                .HasConversion(x => x!.Value, x => x)
                .HasMaxLength(Registration.MAX_LENGTH)
                .IsRequired(false);

            builder.HasIndex(x => x.Registration)
                .IsUnique();

            builder.Property(x => x.Name)
                .HasConversion(x => x.Value, x => x)
                .HasMaxLength(Name.MAX_LENGTH)
                .IsRequired();
           
            builder.OwnsOne(x => x.Address, address =>
            {
                address.Property(x => x.ZipCode)
                    .HasMaxLength(Address.MAX_LENGHT_ZIPCODE)
                    .IsRequired();

                address.Property(x => x.Street)
                    .HasMaxLength(Address.MAX_LENGHT_STREET)
                    .IsRequired();

                address.Property(x => x.Number)
                    .HasMaxLength(Address.MAX_LENGHT_NUMBER)
                    .IsRequired();

                address.Property(x => x.Complement)
                    .HasMaxLength(Address.MAX_LENGHT_COMPLEMENT)
                    .IsRequired();

                address.Property(x => x.Neighborhood)
                    .HasMaxLength(Address.MAX_LENGHT_NEIGHBORHOOD)
                    .IsRequired();

                address.Property(x => x.City)
                    .HasMaxLength(Address.MAX_LENGHT_CITY)
                    .IsRequired();

                address.Property(x => x.State)
                    .HasMaxLength(Address.MAX_LENGHT_STATE)
                    .IsRequired();

                address.Property(x => x.Country)
                    .HasMaxLength(Address.MAX_LENGHT_COUNTRY)
                    .IsRequired();
            });

            builder.OwnsOne(x => x.Contact, contact =>
            {
                contact.Property(x => x.Email)
                    .HasMaxLength(Contact.MAX_LENGHT_EMAIL)
                    .IsRequired();

                contact.Property(x => x.CellPhone)
                    .HasMaxLength(Contact.MAX_LENGHT_PHONE)
                    .IsRequired();
            });

            builder.OwnsMany(x => x.Dependents, deps =>
            {
                deps.Property(x => x.Name)
                    .HasConversion(x => x.Value, x => x)
                    .HasMaxLength(Name.MAX_LENGTH)
                    .IsRequired();

                deps.OwnsOne(x => x.IdCard, idCard =>
                {
                    idCard.Property(x => x.Cpf)
                        .HasConversion(x => x.Number, x => x)
                        .HasMaxLength(CPF.MAX_LENGHT)
                        .IsRequired();

                    idCard.Property(x => x.MotherName)
                        .HasConversion(x => x.Value, x => x)
                        .HasMaxLength(Name.MAX_LENGTH)
                        .IsRequired();

                    idCard.Property(x => x.FatherName)
                        .HasConversion(x => x!.Value, x => x)
                        .HasMaxLength(Name.MAX_LENGTH)
                        .IsRequired();

                    idCard.Property(x => x.BirthCity)
                        .HasMaxLength(IdCard.MAX_LENGHT_BIRTHCITY)
                        .IsRequired();

                    idCard.Property(x => x.BirthState)
                        .HasMaxLength(IdCard.MAX_LENGHT_BIRTHSTATE)
                        .IsRequired();

                    idCard.Property(x => x.Nacionality)
                        .HasMaxLength(IdCard.MAX_LENGHT_NACIONALITY)
                        .IsRequired();

                    idCard.Property(x => x.DateOfBirth)
                        .IsRequired();
                });

                deps.Property(x => x.Gender)
                    .HasConversion(x => x.Name, x => x)
                    .IsRequired();

                deps.Property(x => x.DependencyType)
                    .HasConversion(x => x.Name, x => x)
                    .IsRequired();
            });

            builder.Property(x => x.Status)
                .HasConversion(x => x.Name, x => x)
                .IsRequired();

            builder.Property(x => x.Sip)
                .HasConversion(x => x!.Number, x => x)
                .IsRequired(false);

            builder.OwnsOne(x => x.MedicalAdmissionExam, medical =>
            {
                medical.Property(x => x.DateExam)
                    .IsRequired();
                medical.Property(x => x.Validity)
                    .IsRequired();
            });

            builder.OwnsMany(x => x.Contracts, contracts =>
            {
                contracts.Property(x => x.InitDate)
                    .IsRequired();

                contracts.Property(x => x.FinalDate)
                    .IsRequired();

                contracts.Property(x => x.ContractType)
                    .HasConversion(x => x.Name, x => x)
                    .IsRequired();
            });

            builder.OwnsOne(x => x.PersonalInfo, personal =>
            {
                personal.OwnsOne(x => x.Deficiency, deficiency =>
                {
                    deficiency.Property(x => x.Disabilities)
                        .HasConversion(x => string.Join<Disability>(';', x), x => x.Split(';', StringSplitOptions.None).Select(x => (Disability)x).ToArray())
                        .IsRequired()
                        .Metadata
                        .SetValueComparer(new ValueComparer<ICollection<Disability>>(
                            (c1, c2) => c1!.SequenceEqual(c2!),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                            c => (ICollection<Disability>)c.ToList()));

                    deficiency.Property(x => x.Observation)
                        .HasMaxLength(Deficiency.MAX_OBSERVATION)
                        .IsRequired();
                });

                personal.Property(x => x.MaritalStatus)
                    .HasConversion(x => x.Name, x => x)
                    .IsRequired();

                personal.Property(x => x.Gender)
                    .HasConversion(x => x.Name, x => x)
                    .IsRequired();

                personal.Property(x => x.Ethinicity)
                    .HasConversion(x => x.Name, x => x)
                    .IsRequired();

                personal.Property(x => x.EducationLevel)
                    .HasConversion(x => x.Name, x => x)
                    .IsRequired();
            });

            builder.OwnsOne(x => x.IdCard, idCard =>
            {
                idCard.Property(x => x.Cpf)
                    .HasConversion(x => x.Number, x => x)
                    .HasMaxLength(CPF.MAX_LENGHT)
                    .IsRequired();

                idCard.Property(x => x.MotherName)
                    .HasConversion(x => x.Value, x => x)
                    .HasMaxLength(Name.MAX_LENGTH)
                    .IsRequired();

                idCard.Property(x => x.FatherName)
                    .HasConversion(x => x!.Value, x => x)
                    .HasMaxLength(Name.MAX_LENGTH)
                    .IsRequired();

                idCard.Property(x => x.BirthCity)
                    .HasMaxLength(IdCard.MAX_LENGHT_BIRTHCITY)
                    .IsRequired();

                idCard.Property(x => x.BirthState)
                    .HasMaxLength(IdCard.MAX_LENGHT_BIRTHSTATE)
                    .IsRequired();

                idCard.Property(x => x.Nacionality)
                    .HasMaxLength(IdCard.MAX_LENGHT_NACIONALITY)
                    .IsRequired();

                idCard.Property(x => x.DateOfBirth)
                    .IsRequired();
            });

            builder.Property(x => x.VoteId)
                .HasConversion(x => x!.Number, x => x)
                .HasMaxLength(VoteId.MAX_LENGHT)
                .IsRequired(false);

            builder.OwnsOne(x => x.MilitaryDocument, military =>
            {
                military.Property(x => x.Number)
                    .HasMaxLength(MilitaryDocument.MAX_LENGHT_NUMBER)
                    .IsRequired();

                military.Property(x => x.Type)
                    .HasMaxLength(MilitaryDocument.MAX_LENGHT_TYPE)
                    .IsRequired();
            });

            builder.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .IsRequired()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

            builder.HasOne<Role>()
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .IsRequired(false);

            builder.HasOne<Workplace>()
                .WithMany()
                .HasForeignKey(x => x.WorkPlaceId)
                .IsRequired(false);

        }
    }
}
