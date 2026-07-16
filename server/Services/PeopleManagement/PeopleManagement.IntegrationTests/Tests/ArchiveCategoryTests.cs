using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.ArchiveCategoryCommands.AddListenEvent;
using PeopleManagement.Application.Commands.ArchiveCategoryCommands.CreateArchiveCategory;
using PeopleManagement.Application.Commands.ArchiveCategoryCommands.RemoveListenEvent;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;
using System.Net;

namespace PeopleManagement.IntegrationTests.Tests
{
    [Collection(nameof(IntegrationTestCollection))]
    public class ArchiveCategoryTests(PeopleManagementWebApplicationFactory factory) : BaseIntegrationTest(factory)
    {

        // POST /ArchiveCategory cria a categoria de arquivo com os eventos ouvidos [1,2] e persiste igual à esperada (ToArchiveCategory).
        [Fact]
        public async Task CreateArchiveCategoryWithSuccess()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();
            var client = CreateClient();

            var company = await context.InsertCompany(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var command = new CreateArchiveCategoryModel("Documento Nacional de Identidade", "Documento Nacional Brasileiro que identifica o funcionario.",  [1,2]);

            client.InputHeaders([company.Id]);
            var response = await client.PostAsJsonAsync($"/api/v1/{company.Id}/ArchiveCategory", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(CreateArchiveCategoryResponse)) as CreateArchiveCategoryResponse ?? throw new ArgumentNullException();
            var result = await context.ArchiveCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal(command.ToCommand(company.Id).ToArchiveCategory(result.Id), result);
        }

        // PUT /ArchiveCategory/event/add adiciona o evento ouvido (id 3) a uma categoria existente; o novo id passa a constar em ListenEventsIds.
        [Fact]
        public async Task AddListenEventWithSuccess()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();
            var client = CreateClient();

            var company = await context.InsertCompany(cancellationToken);
            var archiveCategories = await context.InsertArchiveCategory(company.Id, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var listenEvenId = 3;
            var archiveCategory = archiveCategories.First();
            var command = new AddListenEventModel(archiveCategory.Id, [listenEvenId]);

            client.InputHeaders([company.Id]);
            var response = await client.PutAsJsonAsync($"/api/v1/{company.Id}/ArchiveCategory/event/add", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(AddListenEventResponse)) as AddListenEventResponse ?? throw new ArgumentNullException();
            var result = await context.ArchiveCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Contains(listenEvenId, result.ListenEventsIds);
        }

        // PUT /ArchiveCategory/event/remove remove o evento ouvido (id 1) da categoria; ListenEventsIds fica vazio.
        [Fact]
        public async Task RemoveListenEventWithSuccess()
        {
            var cancellationToken = CancellationToken.None;

            var context = GetContext();
            var client = CreateClient();

            var company = await context.InsertCompany(cancellationToken);
            var archiveCategories = await context.InsertArchiveCategory(company.Id, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var listenEvenId = 1;
            var archiveCategory = archiveCategories.First();
            var command = new RemoveListenEventModel(archiveCategory.Id, [listenEvenId]);

            client.InputHeaders([company.Id]);
            var response = await client.PutAsJsonAsync($"/api/v1/{company.Id}/ArchiveCategory/event/remove", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(RemoveListenEventResponse)) as RemoveListenEventResponse ?? throw new ArgumentNullException();
            var result = await context.ArchiveCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == content.Id) ?? throw new ArgumentNullException();
            Assert.Equal([], result.ListenEventsIds);
        }
    }
}
