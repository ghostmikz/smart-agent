using Microsoft.AspNetCore.Mvc;
using SmartSoftware.SmartAgent.Model;
using SmartSoftware.SmartAgent.Utils;

namespace SmartSoftware.SmartAgent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PriceTagController : ControllerBase
    {
        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] PriceTagSendRequest req)
        {
            try
            {
                var result = await PriceTagUtils.SendTags(req);
                return Ok(new { success = true, message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("sendItems")]
        public async Task<IActionResult> SendItems([FromBody] PriceTagSendItemsRequest req)
        {
            try
            {
                var results = await PriceTagUtils.SendItems(req);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
