using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Http;

namespace http_module
{
    public class APIIntegrationModule(IHttpClientFactory httpClientFactory, string apiBaseUrl) : IAPIIntegrationModule
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly Uri _apiBaseUrl = new Uri(apiBaseUrl);

        public async Task<APIBaseResponse<T>> SendRequestAsync<T>(APIBaseRequest request)
        {
            // Validate request parameters
            if (string.IsNullOrEmpty(request.RelativeUrl))
                throw new ArgumentException("Relative URL cannot be null or empty", nameof(request.RelativeUrl));

            // Initialize request message
            var requestMessage = new HttpRequestMessage
            {
                RequestUri = new Uri(_apiBaseUrl, request.RelativeUrl),
                Method = request.Method
            };

            // Handle request content based on type
            if (request.Body != null)
            {
                requestMessage.Content = CreateRequestContent(request);
            }

            // Add headers
            AddHeaders(requestMessage, request.Headers);

            // Send request and handle response
            using var httpClient = _httpClientFactory.CreateClient();
            var httpResponse = await httpClient.SendAsync(requestMessage);

            return await CreateResponseAsync<T>(httpResponse);
        }

        private static HttpContent CreateRequestContent(APIBaseRequest request)
        {
            if (request.IsFormData)
            {
                return CreateFormDataContent(request.Body);
            }

            if (request.Body is Stream streamContent)
            {
                return new StreamContent(streamContent);
            }

            if (request.Body is byte[] byteContent)
            {
                return new ByteArrayContent(byteContent);
            }

            // Default to JSON serialization
            return new StringContent(
                JsonSerializer.Serialize(request.Body),
                Encoding.UTF8,
                request.MediaType ?? "application/json"
            );
        }

        private static MultipartFormDataContent CreateFormDataContent(object formData)
        {
            var content = new MultipartFormDataContent();
            var properties = formData.GetType().GetProperties();

            foreach (var property in properties)
            {
                var value = property.GetValue(formData);
                if (value == null) continue;

                switch (value)
                {
                    case IFormFile file:
                        AddFormFile(content, property.Name, file);
                        break;
                    case Stream stream:
                        content.Add(new StreamContent(stream), property.Name);
                        break;
                    case byte[] bytes:
                        content.Add(new ByteArrayContent(bytes), property.Name);
                        break;
                    default:
                        content.Add(new StringContent(value.ToString() ?? ""), property.Name);
                        break;
                }
            }

            return content;
        }

        private static void AddFormFile(MultipartFormDataContent content, string name, IFormFile file)
        {
            var fileStream = file.OpenReadStream();
            var fileContent = new StreamContent(fileStream);
            
            // Add content type if available
            if (!string.IsNullOrEmpty(file.ContentType))
            {
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            }
            
            content.Add(fileContent, name, file.FileName);
        }

        private static void AddHeaders(HttpRequestMessage requestMessage, Dictionary<string, string>? headers)
        {
            if (headers?.Count > 0)
            {
                foreach (var header in headers)
                {
                    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }

        private async Task<APIBaseResponse<T>> CreateResponseAsync<T>(HttpResponseMessage httpResponse)
        {
            try
            {
                var response = await httpResponse.Content.ReadFromJsonAsync<T>();
                return new APIBaseResponse<T>(
                    httpResponse.IsSuccessStatusCode,
                    (int)httpResponse.StatusCode,
                    response
                );
            }
            catch (Exception)
            {
                return new APIBaseResponse<T>(
                    httpResponse.IsSuccessStatusCode,
                    (int)httpResponse.StatusCode
                );
            }
        }
    }
}
