using DezinsectionApp.Services.AmoCrm.Lead;
using Microsoft.AspNetCore.Mvc;

namespace DezinsectionApp.Controllers.AmoCrm
{
    [ApiController]
    [Route("creatiumapi")]
    public class AmoCrmLeadController(IAmoCrmLeadService amoCrmLeadService) : ControllerBase
    {
        private readonly IAmoCrmLeadService _amoCrmLeadService = amoCrmLeadService;

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

        [HttpPost("notify")]
        public async Task<IActionResult> NotifyMaster()
        {
            try
            {
                var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

                var success = await _amoCrmLeadService.NotifyMaster(requestBody);

                if (success)
                {
                    return Ok("Успешно");
                }
                else
                {
                    return BadRequest("Не удалось уведомить мастера");
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Неверный запрос");
            }
        }
    }
}
