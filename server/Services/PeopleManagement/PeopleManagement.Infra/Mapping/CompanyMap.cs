using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;

namespace PeopleManagement.Infra.Mapping
{
    public class CompanyMap : EntityMap<Company>
    {
        public override void Configure(EntityTypeBuilder<Company> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.CorporateName)
                .HasConversion(x => x.Value, x => x)
                .HasMaxLength(NameCompany.MAX_LENGTH)
                .IsRequired();

            builder.Property(x => x.FantasyName)
                .HasConversion(x => x.Value, x => x)
                .HasMaxLength(NameFantasy.MAX_LENGTH)
                .IsRequired();


            builder.Property(x => x.Cnpj)
                .HasConversion(x => x.Value, x => x)
                .HasMaxLength(CNPJ.MAX_LENGTH)
                .IsRequired();

            builder.OwnsOne(a => a.Contact, x =>
            {
                x.Property(x => x.Email)               
                .HasMaxLength(Contact.MAX_EMAIL)
                .IsRequired();

                x.Property(x => x.Phone)
                .HasMaxLength(Contact.MAX_PHONE)
                .IsRequired();
            });


            builder.OwnsOne(x => x.Address, a =>
            {
                a.Property(v => v.Number)
                .HasMaxLength(Address.MAX_LENGHT_NUMBER)
                .IsRequired();

                a.Property(v => v.Complement)
                .HasMaxLength(Address.MAX_LENGHT_COMPLEMENT)
                .IsRequired();

                a.Property(v => v.Street)
                .HasMaxLength(Address.MAX_LENGHT_STREET)
                .IsRequired();

                a.Property(v => v.Neighborhood)
                .HasMaxLength(Address.MAX_LENGHT_NEIGHBORHOOD)
                .IsRequired();

                a.Property(v => v.City)
                .HasMaxLength(Address.MAX_LENGHT_CITY)
                .IsRequired();

                a.Property(v => v.State)
                .HasMaxLength(Address.MAX_LENGHT_STATE)
                .IsRequired();

                a.Property(v => v.Country)
                .HasMaxLength(Address.MAX_LENGHT_COUNTRY)
                .IsRequired();

                a.Property(v => v.ZipCode)
                .HasMaxLength(Address.MAX_LENGHT_ZIPCODE)
                .IsRequired();
            });

        }
    }
}
