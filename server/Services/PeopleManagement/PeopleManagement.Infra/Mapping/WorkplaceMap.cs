using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate;
using Address = PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate.Address;

namespace PeopleManagement.Infra.Mapping
{
    public class WorkplaceMap : EntityMap<Workplace>
    {
        public override void Configure(EntityTypeBuilder<Workplace> builder)
        {
            base.Configure(builder);

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

            builder.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .IsRequired()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);
        }
    }
}
