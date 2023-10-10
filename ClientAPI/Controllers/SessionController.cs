using ClientAPI.Services;
using ClientSupport.Common.ClientModels;
using Microsoft.AspNetCore.Mvc;

namespace ClientAPI.Controllers
{
    [ApiController]
    [Route("api/session")]
    public class SessionController : ControllerBase
    {
        SessionService _service;
        public SessionController(SessionService service) 
        {
            _service = service;
        }

        [HttpPost("create")]
        public async Task<ActionResult> Create()
        {
            var createSessionResponse = await _service.CreateSession();
            if (createSessionResponse == null || 
                !createSessionResponse.SessionId.HasValue)
                return Ok(new CreateSessionResponse(CreateSessionResponseCode.Failed));

            if (!createSessionResponse.Success)
                return Ok(new CreateSessionResponse(CreateSessionResponseCode.TooBusy));

            return Ok(new CreateSessionResponse(createSessionResponse.SessionId.Value));
        }
        
        [HttpPut("ping")]
        public async Task<ActionResult> Ping(ProlongateSessionRequest prolongateRequest)
        {
            var request = Request;
            var prolongateResponse = await _service.ProlongateSession(prolongateRequest.SessionId);
            if (prolongateResponse == null ||
                !prolongateResponse.Success)
                return Ok(new ProlongateSessionResponse(ProlongateSessionResponseCode.Failed));

            return Ok(new ProlongateSessionResponse(ProlongateSessionResponseCode.Success));
        }
    }
}
