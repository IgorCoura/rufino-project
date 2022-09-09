using System.ComponentModel.DataAnnotations;


namespace BuildManagement.Domain.Models.Provider
{
    public record CreateProviderModel(
        string Name, 
        string Description, 
        string Cnpj, 
        string Email, 
        string Site, 
        string Phone, 
        string Street, 
        string City, 
        string State, 
        string Country, 
        string ZipCode
    );
}
