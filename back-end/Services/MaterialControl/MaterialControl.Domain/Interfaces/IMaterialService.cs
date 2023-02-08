using Commom.Domain.BaseEntities;
using MaterialControl.Domain.Models.Request;
using MaterialControl.Domain.Models.Response;

namespace MaterialControl.Domain.Interfaces
{
    public interface IMaterialService
    {
        Task<MaterialResponse> Create(Context context, CreateMaterialRequest req);
        Task<MaterialResponse> Update(Context context, MaterialRequest req);
        Task Delete(Context context, Guid Id);
        Task<MaterialResponse> Recover(Guid Id);
        Task<IEnumerable<MaterialResponse>> RecoverAll();
    }
}
