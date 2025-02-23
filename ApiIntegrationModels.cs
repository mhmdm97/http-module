namespace http_module;
public class APIBaseRequest(string relativeUrl, HttpMethod method, Dictionary<string, string> headers, object body)
{
    public string RelativeUrl { get; set; } = relativeUrl;
    public HttpMethod Method { get; set; } = method;
    public Dictionary<string, string>? Headers { get; set; } = headers;
    public object? Body { get; set; } = body;
    public string? MediaType { get; set; } = null;
    public bool IsFormData { get; set; } = false;
}
public class APIBaseResponse<T>(bool success = default, int statusCode = default, T? response = default)
{
    public bool Success { get; set; } = success;
    public int StatusCode { get; set; } = statusCode;
    public T? Response { get; set; } = response;
}
