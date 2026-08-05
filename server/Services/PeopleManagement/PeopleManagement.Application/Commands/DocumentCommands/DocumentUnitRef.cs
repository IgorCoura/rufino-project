namespace PeopleManagement.Application.Commands.DocumentCommands
{
    /// <summary>
    /// Endereço de uma unidade de documento. Compartilhado pelas operações que agem sobre um conjunto de unidades
    /// sem carregar mais nada (verificação de snapshot desatualizado, renovação de snapshot).
    /// </summary>
    public record DocumentUnitRef(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId);
}
