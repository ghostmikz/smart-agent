using Microsoft.AspNetCore.Mvc;
using SmartSoftware.SmartAgent.Interfaces;
using SmartSoftware.SmartAgent.Model;
using SmartSoftware.SmartAgent.Utils;

namespace SmartSoftware.SmartAgent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BarcodeScaleController : ControllerBase
    {
        [HttpGet("check")]
        public IActionResult CheckConnection([FromQuery] string ipAddress, [FromQuery] int port)
        {
            try
            {
                bool result = BarcodeScaleUtils.CheckConnection(ipAddress, port);
                return Ok(new { connected = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendDataToScale([FromBody] BarcodeScaleSendRequest data)
        {
            try
            {
                var items = data.Items.Cast<IBarcodeScaleItem>().ToList();
                await BarcodeScaleUtils.SendDataToScale(
                    data.ScaleType,
                    data.IpAddress,
                    data.Port,
                    data.Items,
                    data.ScaleBarCodeType);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
