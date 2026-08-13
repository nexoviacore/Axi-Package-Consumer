using AxiInstallConsumerPackage.Models;

namespace AxiInstallConsumerPackage.Services.Interfaces;



/// <summary>
/// Abstracts token storage.
/// </summary>
public interface ITokenStore
{
    Task SetAsync(string key, CancellationToken ct = default);
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    //Task<bool> UpdateAsync(Action<SessionData> mutate, string sessionId, CancellationToken ct = default); // ← patch in-place
    Task DeleteAsync(string key,CancellationToken ct = default);
    //string GetSessionIdAsync(CancellationToken ct = default);
    //Task<bool> HasTokenAsync(CancellationToken ct = default);
}
