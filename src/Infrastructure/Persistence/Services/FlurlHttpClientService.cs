

namespace Infrastructure.Http;

public class FlurlHttpClientService : IHttpClientService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FlurlHttpClientService> _logger;

    public FlurlHttpClientService(IHttpClientFactory httpClientFactory, ILogger<FlurlHttpClientService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private IFlurlRequest GetRequest(string clientName, string requestUri, IDictionary<string, string>? headers)
    {
        var client = _httpClientFactory.CreateClient(clientName);
        var request = new FlurlRequest(client.BaseAddress + requestUri);

        // Use the FlurlClient directly instead of WithClient
        request.Client = new FlurlClient(client);

        if (headers is not null)
        {
            request.WithHeaders(headers);
        }

        return request;
    }

    public async Task<TResponse?> GetAsync<TResponse>(string clientName, string requestUri, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await GetRequest(clientName, requestUri, headers)
                .SendAsync(HttpMethod.Get, cancellationToken: cancellationToken);

            return await response.GetJsonAsync<TResponse>();
        }
        catch (FlurlHttpException ex)
        {
            _logger.LogError(ex, "HTTP GET request failed for {ClientName} at {RequestUri}", clientName, requestUri);
            return default;
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string clientName, string requestUri, TRequest data, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await GetRequest(clientName, requestUri, headers)
                .SendJsonAsync(HttpMethod.Post, data, cancellationToken: cancellationToken);

            return await response.GetJsonAsync<TResponse>();
        }
        catch (FlurlHttpException ex)
        {
            _logger.LogError(ex, "HTTP POST request failed for {ClientName} at {RequestUri}", clientName, requestUri);
            return default;
        }
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string clientName, string requestUri, TRequest data, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await GetRequest(clientName, requestUri, headers)
                .SendJsonAsync(HttpMethod.Put, data, cancellationToken: cancellationToken);

            return await response.GetJsonAsync<TResponse>();
        }
        catch (FlurlHttpException ex)
        {
            _logger.LogError(ex, "HTTP PUT request failed for {ClientName} at {RequestUri}", clientName, requestUri);
            return default;
        }
    }

    public async Task DeleteAsync(string clientName, string requestUri, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await GetRequest(clientName, requestUri, headers)
                .SendAsync(HttpMethod.Delete, cancellationToken: cancellationToken);
        }
        catch (FlurlHttpException ex)
        {
            _logger.LogError(ex, "HTTP DELETE request failed for {ClientName} at {RequestUri}", clientName, requestUri);
        }
    }
}