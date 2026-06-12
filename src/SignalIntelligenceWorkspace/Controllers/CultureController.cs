using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace SignalIntelligenceWorkspace.Controllers;

[Route("culture")]
public sealed class CultureController : Controller
{
    // The interactive-server render mode fixes the culture when the circuit is created,
    // so a language switch writes a cookie here and triggers a full reload of the
    // returned page, which then renders under the new culture.
    [HttpGet("set")]
    public IActionResult Set(string culture, string redirectUri)
    {
        if (!string.IsNullOrWhiteSpace(culture))
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });
        }

        return LocalRedirect(string.IsNullOrWhiteSpace(redirectUri) ? "/" : redirectUri);
    }
}
