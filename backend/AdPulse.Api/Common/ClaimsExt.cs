using System.Security.Claims;

namespace AdPulse.Api.Common;

public static class ClaimsExt
{
    public static int UserId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public static string UserName(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Name) ?? "";

    public static string DisplayName(this ClaimsPrincipal user) =>
        user.FindFirstValue("display_name") ?? "";

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(Models.Roles.Admin);
}