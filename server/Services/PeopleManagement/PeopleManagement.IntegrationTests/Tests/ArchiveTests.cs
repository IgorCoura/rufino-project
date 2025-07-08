using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.ArchiveCategoryCommands.AddListenEvent;
using PeopleManagement.Application.Commands.ArchiveCommands.InsertFile;
using PeopleManagement.Application.Commands.ArchiveCommands.NotApplicable;
using PeopleManagement.Application.Commands.DocumentCommands.InsertDocument;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;
using System.Net;
using System.Net.Http.Headers;

namespace PeopleManagement.IntegrationTests.Tests
{
    public class ArchiveTests(PeopleManagementWebApplicationFactory factory) : IClassFixture<PeopleManagementWebApplicationFactory>
    {
        private readonly PeopleManagementWebApplicationFactory _factory = factory;

        [Fact]
        public async Task InsertPdfWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var company = await context.InsertCompany(cancellationToken);
            var archiveCategories = await context.InsertArchiveCategory(company.Id, cancellationToken);
            var archive = await context.InsertArchive(company.Id, archiveCategories.First().Id, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            using var content = new MultipartFormDataContent();

            // Carregue o arquivo PDF em um stream
            var path = Path.Combine("DataForTests", "199f760b-601d-4a05-aee4-d0a9dbcc6b4d.pdf");
            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var streamContent = new StreamContent(fileStream);

            // Adicione o conteúdo do tipo arquivo ao multipart/form-data
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(streamContent, "formFile", Path.GetFileName(path));

            content.Add(new StringContent(archive.OwnerId.ToString()), "ownerId");
            content.Add(new StringContent(archive.CategoryId.ToString()), "categoryId");

            client.InputHeaders([company.Id]);
            var response = await client.PostAsync($"/api/v1/{company.Id}/archive/file", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var contentResponse = await response.Content.ReadFromJsonAsync(typeof(InsertFileResponse)) as InsertFileResponse ?? throw new ArgumentNullException();
            var result = await context.Archives.AsNoTracking().FirstOrDefaultAsync(x => x.Id == contentResponse.Id) ?? throw new ArgumentNullException();
            var documentResponse = result.Files.First();
            Assert.Equal(Extension.PDF, documentResponse.Extension);

            using var scope = _factory.Services.CreateScope();

            var blobService = scope.ServiceProvider.GetRequiredService<IBlobService>();

            var stream = await blobService.DownloadAsync(documentResponse.GetNameWithExtension, result.CompanyId.ToString(), cancellationToken);

            Assert.NotNull(stream);
            Assert.True(stream.Length > 0);

        }

        [Fact]
        public async Task FileNotApplicableWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var company = await context.InsertCompany(cancellationToken);
            var archiveCategories = await context.InsertArchiveCategory(company.Id, cancellationToken);
            var archive = await context.InsertArchiveOneFilePending(company.Id, archiveCategories.First().Id, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);

            var command = new FileNotApplicableModel(archive.Id, archive.OwnerId, archive.Files.First().Name.Value);

            client.InputHeaders([company.Id]);
            var response = await client.PutAsJsonAsync($"/api/v1/{company.Id}/archive/file/notapplicable", command);

            var res = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<FileNotApplicableResponse>();
            var result = await context.Archives.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content!.Id) ?? throw new ArgumentNullException();
            Assert.Equal(FileStatus.NotApplicable, result.Files.First().Status);
        }


    }
}
