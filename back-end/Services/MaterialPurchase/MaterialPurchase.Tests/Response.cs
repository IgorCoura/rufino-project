namespace MaterialPurchase.Tests
{
    public class Response<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
    }
}
