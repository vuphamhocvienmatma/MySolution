namespace Application.Common.Interfaces;

public interface IHttpClientService
{
    Task<TResponse?> GetAsync<TResponse>(
        string clientName,
        string requestUri,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);

    Task<TResponse?> PostAsync<TRequest, TResponse>(
        string clientName,
        string requestUri,
        TRequest data,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);

    Task<TResponse?> PutAsync<TRequest, TResponse>(
        string clientName,
        string requestUri,
        TRequest data,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string clientName,
        string requestUri,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);
}