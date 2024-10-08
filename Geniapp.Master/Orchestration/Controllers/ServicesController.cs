using Geniapp.Master.Orchestration.Models;
using Geniapp.Master.Orchestration.Services;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace Geniapp.Master.Orchestration.Controllers;

[Route("/admin/services")]
[OpenApiTag("Services")]
[ApiController]
public class ServicesController : ControllerBase
{
    [HttpGet("frontend")]
    public IReadOnlyCollection<FrontendServiceInformation> GetFrontendServices([FromServices] FrontendsService frontendsService) => frontendsService.GetFrontendServices();

    [HttpGet("worker")]
    public IReadOnlyCollection<WorkerServiceInformation> GetWorkerServices([FromServices] WorkersService workerServices) => workerServices.GetWorkerServices();
}
