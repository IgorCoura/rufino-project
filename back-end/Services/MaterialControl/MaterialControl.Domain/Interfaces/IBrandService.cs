using Commom.Domain.BaseEntities;
using MaterialControl.Domain.Models.Request;
using MaterialControl.Domain.Models.Response;

namespace MaterialControl.Domain.Interfaces
{
    public interface IBrandService
    {
        Task<BrandResponse> Create(Context context, CreateBrandRequest req);
        Task<BrandResponse> Update(Context context, BrandRequest req);
        Task Delete(Context context, Guid Id);
        Task<BrandResponse> Recover(Guid Id);
        Task<IEnumerable<BrandResponse>> RecoverAll();
    }
}
