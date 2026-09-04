#pragma warning disable CS0618 // O arquivo INTEIRO e a feature Archive, descontinuada.
// Os tipos seguem marcados com [Obsolete] para quem estiver de fora; aqui dentro o aviso
// so produziria ruido no build de uma feature que ninguem deve mexer.
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate;
using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using Name = PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.Name;

namespace PeopleManagement.Infra.Mapping
{
    [Obsolete("Feature Archive descontinuada em 2026-09-04: o desenvolvimento parou no meio e os endpoints foram removidos. Nao estenda nem use em codigo novo; ver o plano de refatoracao de autorizacao no CLAUDE.md.")]
    public class ArchiveMap : EntityMap<Archive>
    {
        public override void Configure(EntityTypeBuilder<Archive> builder)
        {
            base.Configure(builder);

            builder.OwnsMany(x => x.Files, file =>
            {
                file.HasKey("Id");

                file.Property(x => x.Name)
                    .HasConversion(x => x.Value, x => x)
                    .HasMaxLength(Name.MAX_LENGTH)
                    .IsRequired();

                file.Property(x => x.Extension)
                    .HasConversion(x => x.Id, x => x)
                    .IsRequired(); 

                file.Property(x => x.Status)
                    .HasConversion(x => x.Id, x => x)
                    .IsRequired();

                file.Property(x => x.InsertAt)
                    .IsRequired();
            });
            
            builder.HasIndex(x => x.OwnerId);

            builder.Property(x => x.OwnerId)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion(x => x.Id, x => x)
                .IsRequired();

            builder.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .IsRequired()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

            builder.HasOne<ArchiveCategory>()
               .WithMany()
               .HasForeignKey(x => x.CategoryId)
               .IsRequired()
               .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

        }
    }
}
