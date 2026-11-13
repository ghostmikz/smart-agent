using Microsoft.AspNetCore.Mvc;
using SmartSoftware.SmartAgent.Model;
using SmartSoftware.SmartAgent.Utils;

namespace SmartSoftware.SmartAgent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RFIDPrinterController : ControllerBase
    {
        [HttpPost("print")]
        public async Task<IActionResult> Print([FromBody] RFIDPrintRequest req)
        {
            try
            {
                var imageBytes = Convert.FromBase64String(req.ImageBase64);

                var result = req.PrinterType switch
                {
                    "1000" => await RFIDPrinterUtils.SendCP30("1000", req.RfidId, imageBytes), // CP30
                    "1001" => await RFIDPrinterUtils.SendZT411(req.RfidId, imageBytes),          // ZT411
                    _ => throw new Exception($"RFID: дэмжигдэхгүй принтерийн төрөл ({req.PrinterType})")
                };

                if (result.Code != 0)
                    return StatusCode(500, new { error = result.Msg });

                return Ok(new { success = true, message = result.Msg });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
