using ClientSupport.Common.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClientSupportService.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SessionController : ControllerBase
    {
        private readonly ISessionManager _sessionManager;
        public SessionController(ISessionManager sessionManager) {
            _sessionManager = sessionManager;
        }

        [HttpPost("create")]
        public async Task<ActionResult> Create(CreateSessionCommand createCommand)
        {
            var session = await _sessionManager.CreateSessionAsync();
            return Ok(new CreateSessionCommandResponse() { Success = session.HasValue, SessionId = session });
        }

        [HttpPut("prolongate")]
        public async Task<ActionResult> Prolongate(ProlongateSessionCommand prolongateCommand)
        {
            var result = await _sessionManager.ProlongateSessionAsync(prolongateCommand.SessionId);
            return Ok(new ProlongateSessionCommandResponse() { Success = result });
        }
    }
}
