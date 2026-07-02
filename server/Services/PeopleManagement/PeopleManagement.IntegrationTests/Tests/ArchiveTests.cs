using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.ArchiveCommands.InsertFile;
using PeopleManagement.Application.Commands.ArchiveCommands.NotApplicable;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;
using System.Net;

namespace PeopleManagement.IntegrationTests.Tests
{
    [Collection(nameof(IntegrationTestCollection))]
    public class ArchiveTests(PeopleManagementWebApplicationFactory factory) : BaseIntegrationTest(factory)
    {

        // POST /archive/file faz upload de um PDF para um Archive existente: grava o arquivo (Extension.PDF) e disponibiliza o blob no storage (download > 0 bytes).
        [Fact]
        public async Task InsertPdfWithSuccess()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();
            var client = CreateClient();

            var company = await context.InsertCompany(cancellationToken);
            var archiveCategories = await context.InsertArchiveCategory(company.Id, cancellationToken);
            var archive = await context.InsertArchive(company.Id, archiveCategories.First().Id, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            using var content = PdfMultipartContent(
                ("ownerId", archive.OwnerId.ToString()),
                ("categoryId", archive.CategoryId.ToString()));

            client.InputHeaders([company.Id]);
            var response = await client.PostAsync($"/api/v1/{company.Id}/archive/file", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var contentResponse = await response.Content.ReadFromJsonAsync<InsertFileResponse>() ?? throw new ArgumentNullException();
            var result = await context.Archives.AsNoTracking().FirstOrDefaultAsync(x => x.Id == contentResponse.Id) ?? throw new ArgumentNullException();
            var documentResponse = result.Files.First();
            Assert.Equal(Extension.PDF, documentResponse.Extension);

            await AssertBlobExistsAsync(documentResponse.GetNameWithExtension, result.CompanyId, cancellationToken);
        }

        // PUT /archive/file/notapplicable marca um arquivo pendente como não aplicável; o status do arquivo passa a FileStatus.NotApplicable.
        [Fact]
        public async Task FileNotApplicableWithSuccess()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();
            var client = CreateClient();

            var company = await context.InsertCompany(cancellationToken);
            var archiveCategories = await context.InsertArchiveCategory(company.Id, cancellationToken);
            var archive = await context.InsertArchiveOneFilePending(company.Id, archiveCategories.First().Id, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new FileNotApplicableModel(archive.Id, archive.OwnerId, archive.Files.First().Name.Value);

            client.InputHeaders([company.Id]);
            var response = await client.PutAsJsonAsync($"/api/v1/{company.Id}/archive/file/notapplicable", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<FileNotApplicableResponse>();
            var result = await context.Archives.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content!.Id) ?? throw new ArgumentNullException();
            Assert.Equal(FileStatus.NotApplicable, result.Files.First().Status);
        }


    }
}
