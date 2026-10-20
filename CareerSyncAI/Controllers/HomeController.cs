using Microsoft.AspNetCore.Mvc;

namespace CareerSyncAI.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Error() => View();
    public IActionResult About() => View();
    public IActionResult FAQs() => View();
    
}
