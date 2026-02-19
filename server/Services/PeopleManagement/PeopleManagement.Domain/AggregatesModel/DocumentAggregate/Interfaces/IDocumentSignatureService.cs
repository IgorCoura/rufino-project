using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Models;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using System.Text.Json.Nodes;

namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces
{
    public interface IDocumentSignatureService
    {
        Task<DocumentSignatureModel> SendToSignatureWithWhatsapp(Stream documentStream, Guid documentUnitId, Document document, Company company, Employee employee, PlaceSignature[] placeSignatures,
            DateTime dateLimitToSign, int eminderEveryNDays, CancellationToken cancellationToken = default);

        Task<DocumentSignatureModel> SendToSignatureWithSelfie(Stream documentStream, Guid documentUnitId, Document document, Company company,
            Employee employee, PlaceSignature[] placeSignatures, DateTime dateLimitToSign, int eminderEveryNDays, CancellationToken cancellationToken = default);
        Task<DocumentSignatureModel> SendToSignatureWithSMS(Stream documentStream, Guid documentUnitId, Document document, Company company,
            Employee employee, PlaceSignature[] placeSignatures, DateTime dateLimitToSign, int eminderEveryNDays, CancellationToken cancellationToken = default);

    }
}

