using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using RequestReceiver.Services.RabbitMq;
using System.Text;
using System.Text.Json;

namespace RequestReceiver.Controllers.AmoCrm
{
    [ApiController]
    [Route("creatiumapi")]
    public class AmoCrmLeadController(RabbitMqService rabbitMqService) : ControllerBase
    {
        private readonly IChannel _channel = rabbitMqService.GetChannel();

        [Authorize]
        [HttpPost("post")]
        public async Task<IActionResult> Post()
        {
            return await ProcessRequestAsync("post");
        }

        [HttpPost("assignmaster")]
        public async Task<IActionResult> AssignMaster()
        {
            return await ProcessRequestAsync("assignmaster");
        }

        [Authorize]
        [HttpPost("notify")]
        public async Task<IActionResult> Notify()
        {
            return await ProcessRequestAsync("notify");
        }

        private async Task<IActionResult> ProcessRequestAsync(string endpointType)
        {
            try
            {
                var stringRequestBody = await new StreamReader(Request.Body).ReadToEndAsync();

                var message = new
                {
                    EndpointType = endpointType,
                    Body = stringRequestBody
                };
                var messageBody = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(messageBody);

                await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: "AmoQueue", body: body);

                return Ok("Успешно");
            }
            catch (Exception)
            {
                return BadRequest("Неверный запрос");
            }
        }
    }
}
