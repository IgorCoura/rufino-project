using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;

namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces
{
    public interface IPdfService
    {
        Task<byte[]> ConvertHtml2Pdf(TemplateFileInfo type, string content, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<(Guid DocumentUnitId, byte[] Pdf)>> ConvertHtml2PdfRange(
            IEnumerable<(Guid DocumentUnitId, TemplateFileInfo Template, string Content)> items,
            CancellationToken cancellationToken = default);
        void InvalidateTemplateCache(string templateDirectory);
    }
}
