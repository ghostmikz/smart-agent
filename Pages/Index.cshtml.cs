using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SmartSoftware.DeviceAgent.Pages
{
    public class IndexModel : PageModel
    {
        public string Status { get; set; } = "OK";

        public void OnGet()
        {
        }
    }
}
