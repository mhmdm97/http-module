namespace http_module
{
    public interface IAPIIntegrationModule
    {
        public Task<APIBaseResponse<T>> SendRequestAsync<T>(APIBaseRequest request);
    }
}
