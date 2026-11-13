using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SmartSoftware.SmartAgent.Pages
{
    public class IndexModel : PageModel
    {
        public string Status { get; set; } = "OK";

        public void OnGet()
        {
        }
    }
}
