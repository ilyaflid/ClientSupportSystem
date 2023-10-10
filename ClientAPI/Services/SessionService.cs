using ClientSupport.Common.ClientModels;
using ClientSupport.Common.DTO;

namespace ClientAPI.Services
{
    public class SessionService
    {
        private readonly HttpClient _client;
        private readonly ConfigurationSettingsProvider _settingsProvider;
        public SessionService(ConfigurationSettingsProvider settingsProvider) {
            _client = new HttpClient();
            _settingsProvider = settingsProvider;
            _client.BaseAddress = new Uri(_settingsProvider.Host);
        }

        public async Task<CreateSessionCommandResponse?> CreateSession()
        {
            using var response = await _client.PostAsJsonAsync(
                _settingsProvider.CreateSessionPath, new CreateSessionCommand());           
            return await response.Content.ReadFromJsonAsync<CreateSessionCommandResponse>();
        }

        public async Task<ProlongateSessionCommandResponse?> ProlongateSession(Guid sessionId)
        {
            using var response = await _client.PutAsJsonAsync(
                _settingsProvider.ProlongateSessionPath,
                new ProlongateSessionCommand() { SessionId = sessionId } );
            return await response.Content.ReadFromJsonAsync<ProlongateSessionCommandResponse>();
        }
    }
}
