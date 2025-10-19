using Microsoft.AspNetCore.Http;

namespace SmartAuth.Tests.Helpers;

public sealed class FixedHttpContextAccessor : IHttpContextAccessor
{
    public HttpContext? HttpContext { get; set; }
}

