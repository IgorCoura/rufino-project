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
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.FantasyName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.Cnpj)
                .HasMaxLength(18)
                .IsRequired();

            builder.Property(x => x.Email)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Phone)
                .HasMaxLength(20);


            builder.OwnsOne(x => x.Address, a =>
            {
                a.Property(v => v.Street)
                .HasMaxLength(100)
                .IsRequired();

                a.Property(v => v.City)
                .HasMaxLength(50)
                .IsRequired();

                a.Property(v => v.State)
                .HasMaxLength(50)
                .IsRequired();

                a.Property(v => v.Country)
                .HasMaxLength(50)
                .IsRequired();

                a.Property(v => v.ZipCode)
                .HasMaxLength(16)
                .IsRequired();
            });
        }
    }
}
