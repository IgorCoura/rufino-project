using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentGroupAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using Name = PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Name;
using Description = PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Description;
using System.Text.Json;

namespace PeopleManagement.Infra.Mapping
{
    public class DocumentTemplateMap : EntityMap<DocumentTemplate>
    {
        public override void Configure(EntityTypeBuilder<DocumentTemplate> builder)
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

            builder.Property(x => x.DocumentValidityDuration)
                .IsRequired(false);

            builder.Property(x => x.Workload)
               .IsRequired(false);

            // AcceptsSignature e PlaceSignatures são derivados da SignaturePolicy — presença da policy = aceita
            // assinatura, e os locais moram dentro dela. Mapear qualquer um dos dois duplicaria no banco o que
            // já vive no jsonb da policy.
            builder.Ignore(x => x.AcceptsSignature);
            builder.Ignore(x => x.PlaceSignatures);

            builder.Property(x => x.UsePreviousPeriod)
                .IsRequired();

            builder.OwnsMany(x => x.Policies, policy =>
            {
                policy.ToTable("DocumentTemplatePolicies");

                policy.Property(x => x.Type)
                    .HasConversion(x => x.Id, x => x)
                    .IsRequired();

                policy.Property(x => x.Params)
                    .HasColumnType("jsonb")
                    .IsRequired();
            });

            builder.Navigation(x => x.Policies)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.OwnsOne(x => x.TemplateFileInfo, t =>
            {
                t.Property(x => x.Directory)
                .HasConversion(x => x.Value, x => x)
                .HasMaxLength(DirectoryName.MAX_LENGTH)
                .IsRequired();

                t.Property(x => x.BodyFileName)
                    .HasConversion(x => x.Value, x => x)
                    .HasMaxLength(FileName.MAX_LENGTH)
                    .IsRequired();

                t.Property(x => x.HeaderFileName)
                    .HasConversion(x => x.Value, x => x)
                    .HasMaxLength(FileName.MAX_LENGTH)
                    .IsRequired();

                t.Property(x => x.FooterFileName)
                    .HasConversion(x => x.Value, x => x)
                    .HasMaxLength(FileName.MAX_LENGTH)
                    .IsRequired();

                t.Property(x => x.RecoversDataType)
                   .HasConversion(
                        v => JsonSerializer.Serialize(v.Select(r => r.Id), JsonSerializerOptions.Default), // salvar apenas a lista de Ids
                        v => JsonSerializer.Deserialize<List<int>>(v, JsonSerializerOptions.Default)!
                                    .Select(id => RecoverDataType.FromValue<RecoverDataType>(id))
                                    .ToList(),
                        new ValueComparer<List<RecoverDataType>>(
                            (c1, c2) => c1!.SequenceEqual(c2!),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                            c => c.ToList()));

            });

            builder.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .IsRequired()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

            builder.HasOne<DocumentGroup>()
                .WithMany()
                .HasForeignKey(x => x.DocumentGroupId)
                .IsRequired()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

        }
    }
}
