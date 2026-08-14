using System.Net.Http.Json;

namespace Niuro.Loans.Infrastructure.ExternalService;

internal interface IExternalLoanService
{
    Task CreateAsync(CustomerSyncPayload payload, CancellationToken cancellationToken);

    Task UpdateAsync(CustomerSyncPayload payload, CancellationToken cancellationToken);
}

/// <summary>
/// Talks to the partner system over HTTP.
/// <para>
/// Two methods rather than one with a flag, because the two cases really are different
/// requests: a first application is a POST, a repeat application is a PUT against the
/// record we already created.
/// </para>
/// </summary>
internal sealed class ExternalLoanServiceClient(HttpClient httpClient) : IExternalLoanService
{
    public Task CreateAsync(CustomerSyncPayload payload, CancellationToken cancellationToken) =>
        SendAsync(() => httpClient.PostAsJsonAsync("customers", payload, cancellationToken));

    public Task UpdateAsync(CustomerSyncPayload payload, CancellationToken cancellationToken) =>
        SendAsync(() => httpClient.PutAsJsonAsync($"customers/{payload.CustomerId}", payload, cancellationToken));

    /// <summary>
    /// Throws on any non-success status. The caller is the outbox dispatcher, and a thrown
    /// exception is exactly what tells it to leave the message pending and try again.
    /// </summary>
    private static async Task SendAsync(Func<Task<HttpResponseMessage>> send)
    {
        using var response = await send();
        response.EnsureSuccessStatusCode();
    }
}
