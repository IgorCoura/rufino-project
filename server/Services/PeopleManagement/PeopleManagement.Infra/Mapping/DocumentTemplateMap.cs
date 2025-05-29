using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
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

            builder.OwnsMany(x => x.PlaceSignatures, prop =>
            {
                prop.Property(x => x.Type)
                    .HasConversion(x => x.Id, x => x)
                    .IsRequired();

                prop.Property(x => x.Page)
                    .HasConversion(x => x.Value, x => x)
                    .IsRequired();

                prop.Property(x => x.RelativePositionBotton)
                    .HasConversion(x => x.Value, x => x)
                    .IsRequired();

                prop.Property(x => x.RelativePositionLeft)
                    .HasConversion(x => x.Value, x => x)
                    .IsRequired();

                prop.Property(x => x.RelativeSizeX)
                    .HasConversion(x => x.Value, x => x)
                    .IsRequired();

                prop.Property(x => x.RelativeSizeY)
                    .HasConversion(x => x.Value, x => x)
                    .IsRequired();

            });

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

        }
    }
}
