using Commom.Domain.BaseEntities;
using MaterialControl.Domain.Models.Request;
using MaterialControl.Domain.Models.Response;

namespace MaterialControl.Domain.Interfaces
{
    public interface IUnityService
    {
        Task<UnityResponse> Create(Context context, CreateUnityRequest req);
        Task<UnityResponse> Update(Context context, UnityRequest req);
        Task Delete(Context context, Guid Id);
        Task<UnityResponse> Recover(Guid Id);
        Task<IEnumerable<UnityResponse>> RecoverAll();
    }
}
