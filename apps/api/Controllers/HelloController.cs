using Microsoft.AspNetCore.Mvc;

namespace MemberOrgApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HelloController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = "Hello from C# API!" });
    }
}