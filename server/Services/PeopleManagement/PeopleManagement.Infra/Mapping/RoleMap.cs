﻿using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate;

namespace PeopleManagement.Infra.Mapping
{
    public class RoleMap : EntityMap<Role>
    {
        public override void Configure(EntityTypeBuilder<Role> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Name)
                .HasConversion(x => x.Value, x => x)
                .HasMaxLength(Name.MAX_LENGTH)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasConversion(x => x.Value, x => x)
                .HasMaxLength(Description.MAX_LENGTH)
                .IsRequired();

            builder.Property(x => x.CBO)
                .HasConversion(x => x.Value, x => x)
                .HasMaxLength(CBO.MAX_LENGTH)
                .IsRequired();

            builder.OwnsOne(x => x.Remuneration, remuneration =>
            {
                remuneration.Property(x => x.PaymentUnit)
                        .HasConversion(x => x.Id, x => x)
                        .IsRequired();

                remuneration.OwnsOne(x => x.BaseSalary, salary =>
                {
                    salary.Property(x => x.Type)
                        .HasConversion(x => x.Id, x => x)
                        .IsRequired();

                    salary.Property(x => x.Value)
                        .HasMaxLength(Currency.MAX_LENGTH)
                        .IsRequired();
                });
            });

            builder.HasOne(x => x.Position)
                .WithMany()
                .IsRequired();
        }
    }
}