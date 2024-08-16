using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.ArchiveCategoryCommands.AddListenEvent;
using PeopleManagement.Application.Commands.ArchiveCategoryCommands.CreateArchiveCategory;
using PeopleManagement.Application.Commands.ArchiveCategoryCommands.RemoveListenEvent;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;
using System.Net;

namespace PeopleManagement.IntegrationTests.Tests
{
    public class ArchiveCategoryTests(PeopleManagementWebApplicationFactory factory) : IClassFixture<PeopleManagementWebApplicationFactory>
    {
        private readonly PeopleManagementWebApplicationFactory _factory = factory;

        [Fact]
        public async Task CreateArchiveCategoryWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var company = await context.InsertCompany(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new CreateArchiveCategoryCommand("Documento Nacional de Identidade", "Documento Nacional Brasileiro que identifica o funcionario.", company.Id, [1,2]);

            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            var response = await client.PostAsJsonAsync("/api/v1/ArchiveCategory/Create", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(CreateArchiveCategoryResponse)) as CreateArchiveCategoryResponse ?? throw new ArgumentNullException();
            var result = await context.ArchiveCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.ToArchiveCategory(result.Id), result);
        }

        [Fact]
        public async Task AddListenEventWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var company = await context.InsertCompany(cancellationToken);
            var archiveCategories = await context.InsertArchiveCategory(company.Id, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var listenEvenId = 3;
            var archiveCategory = archiveCategories.First();
            var command = new AddListenEventCommand(archiveCategory.Id, archiveCategory.CompanyId, [listenEvenId]);

            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            var response = await client.PutAsJsonAsync("/api/v1/ArchiveCategory/ListenEvent/Add", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(AddListenEventResponse)) as AddListenEventResponse ?? throw new ArgumentNullException();
            var result = await context.ArchiveCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Contains(listenEvenId, result.ListenEventsIds);
        }

        [Fact]
        public async Task RemoveListenEventWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var company = await context.InsertCompany(cancellationToken);
            var archiveCategories = await context.InsertArchiveCategory(company.Id, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var listenEvenId = 1;
            var archiveCategory = archiveCategories.First();
            var command = new RemoveListenEventCommand(archiveCategory.Id, archiveCategory.CompanyId, [listenEvenId]);

            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            var response = await client.PutAsJsonAsync("/api/v1/ArchiveCategory/ListenEvent/Remove", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(RemoveListenEventResponse)) as RemoveListenEventResponse ?? throw new ArgumentNullException();
            var result = await context.ArchiveCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal([], result.ListenEventsIds);
        }
    }
}
