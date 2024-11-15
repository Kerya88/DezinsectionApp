using DezinsectionApp.Services.AmoCrm.Lead;
using Microsoft.AspNetCore.Mvc;

namespace DezinsectionApp.Controllers.AmoCrm
{
    [ApiController]
    [Route("creatiumapi")]
    public class AmoCrmLeadController : ControllerBase
    {
        private readonly IAmoCrmLeadService _amoCrmLeadService;

        private readonly ILogger<AmoCrmLeadController> _logger;

        public AmoCrmLeadController(IAmoCrmLeadService amoCrmLeadService, ILogger<AmoCrmLeadController> logger)
        {
            _amoCrmLeadService = amoCrmLeadService;
            _logger = logger;
        }

        [HttpPost("post")]
        public async Task<IActionResult> Post()
        {
            try
            {
                var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

                _ = Task.Run(async () => await _amoCrmLeadService.AcceptLead(requestBody));

                return Ok("Успешно");
            }
            catch (Exception)
            {
                return BadRequest("Неверный запрос");
            }
        }

        [HttpPost("assignmaster")]
        public async Task<IActionResult> AssignMaster()
        {
            try
            {
                var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

                _ = Task.Run(async () => await _amoCrmLeadService.AssignMaster(requestBody));

                return Ok("Успешно");
            }
            catch (Exception)
            {
                return BadRequest("Неверный запрос");
            }
        }
    }
}
