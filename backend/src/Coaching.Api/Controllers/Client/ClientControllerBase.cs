using Coaching.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coaching.Api.Controllers.Client;

/// <summary>
/// Mọi controller của Web Học viên kế thừa lớp này: route /api/client/*, chỉ role Student.
/// Dữ liệu luôn lọc theo học viên trong token (BR-13), không nhận StudentId từ request.
/// </summary>
[ApiController]
[Route("api/client/[controller]")]
[Authorize(Policy = Policies.StudentOnly)]
public abstract class ClientControllerBase : ControllerBase;
