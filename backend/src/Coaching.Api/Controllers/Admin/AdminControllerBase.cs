using Coaching.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coaching.Api.Controllers.Admin;

/// <summary>Mọi controller của Web Admin kế thừa lớp này: route /api/admin/*, chỉ role Admin.</summary>
[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Policy = Policies.AdminOnly)]
public abstract class AdminControllerBase : ControllerBase;
