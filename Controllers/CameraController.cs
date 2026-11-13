using Microsoft.AspNetCore.Mvc;
using SmartSoftware.DeviceAgent.Model;
using SmartSoftware.DeviceAgent.Utils;

namespace SmartSoftware.DeviceAgent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CameraController : ControllerBase
    {
        [HttpPost("devices")]
        public async Task<IActionResult> GetDevices([FromBody] CameraDeviceListRequest req)
        {
            try
            {
                var devices = await CameraUtils.GetDevicesAsync(req);
                return Ok(devices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("play")]
        public async Task<IActionResult> Play([FromBody] CameraPlayRequest req)
        {
            try
            {
                var fileName = await CameraUtils.Play(req);
                return Ok(new { fileName, url = $"/api/Camera/video/{fileName}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("video/{fileName}")]
        public IActionResult GetVideo(string fileName)
        {
            var safeName = Path.GetFileName(fileName);
            var filePath = Path.Combine(CameraUtils.OutputDir, safeName);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var contentType = safeName.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase) ? "video/x-matroska" : "video/mp4";
            var stream = System.IO.File.OpenRead(filePath);
            return File(stream, contentType, enableRangeProcessing: true);
        }
    }
}
