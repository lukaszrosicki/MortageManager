using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MortgagePro.WebUI.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
    
    [Authorize]
    public IActionResult Simulator() => View();
    
    public IActionResult Documentation() => View();
}
