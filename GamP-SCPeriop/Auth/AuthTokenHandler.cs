using System.Net.Http.Headers;
using Blazored.SessionStorage;

namespace GamP_SCPeriop.Client.Auth
{
    public class AuthTokenHandler : DelegatingHandler
    {
        private readonly ISessionStorageService _sessionStorage;
        private const string TokenKey = "authToken";

        public AuthTokenHandler(ISessionStorageService sessionStorage)
        {
            _sessionStorage = sessionStorage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _sessionStorage.GetItemAsync<string>(TokenKey);

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}