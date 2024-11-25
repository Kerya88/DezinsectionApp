using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Text;
using System.Threading.Channels;

namespace RequestReceiver.Controllers.AmoCrm
{
    [ApiController]
    [Route("creatiumapi")]
    public class AmoCrmLeadController : ControllerBase
    {
        private IConnection _connection;
        private IChannel _channel;

        public AmoCrmLeadController()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            _connection = factory.CreateConnectionAsync().Result;
            _channel = _connection.CreateChannelAsync().Result;
            _channel.QueueDeclareAsync(queue: "AmoQueue", durable: true, exclusive: false, autoDelete: false, arguments: null);
        }

        [HttpPost("post")]
        public async Task<IActionResult> Post()
        {
            try
            {
                var stringRequestBody = await new StreamReader(Request.Body).ReadToEndAsync();
                var byteRequestBody = Encoding.UTF8.GetBytes(stringRequestBody);

                await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: "AmoQueue", body: byteRequestBody);

                return Ok("Успешно");
            }
            catch (Exception)
            {
                return BadRequest("Неверный запрос");
            }
        }

        //[HttpPost("assignmaster")]
        //public async Task<IActionResult> AssignMaster()
        //{
        //    try
        //    {
        //        var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();
        //
        //        _ = Task.Run(async () => await _amoCrmLeadService.AssignMaster(requestBody));
        //
        //        return Ok("Успешно");
        //    }
        //    catch (Exception)
        //    {
        //        return BadRequest("Неверный запрос");
        //    }
        //}
        //
        //[HttpPost("notify")]
        //public async Task<IActionResult> NotifyMaster()
        //{
        //    try
        //    {
        //        var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();
        //
        //        var success = await _amoCrmLeadService.NotifyMaster(requestBody);
        //
        //        if (success)
        //        {
        //            return Ok("Успешно");
        //        }
        //        else
        //        {
        //            return BadRequest("Не удалось уведомить мастера");
        //        }
        //
        //    }
        //    catch (Exception)
        //    {
        //        return BadRequest("Неверный запрос");
        //    }
        //}
    }
}
