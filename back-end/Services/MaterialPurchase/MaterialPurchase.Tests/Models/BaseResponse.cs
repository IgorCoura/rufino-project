namespace MaterialPurchase.Tests.Models
{
    public class BaseResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
    }
}
