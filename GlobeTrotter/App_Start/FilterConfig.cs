using System;
using System.Web;
using System.Web.Mvc;

namespace GlobeTrotter
{
    /// <summary>
    /// Adds Cache-Control: no-cache, no-store, must-revalidate headers to every response
    /// for authenticated users, preventing the browser back-button from showing
    /// protected pages after the user has logged out.
    /// </summary>
    public class NoCacheForAuthenticatedFilter : ActionFilterAttribute
    {
        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (filterContext.HttpContext.User != null && filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                filterContext.HttpContext.Response.Cache.SetCacheability(HttpCacheability.NoCache);
                filterContext.HttpContext.Response.Cache.SetNoStore();
                filterContext.HttpContext.Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
                filterContext.HttpContext.Response.Cache.AppendCacheExtension("must-revalidate, proxy-revalidate");
            }
            base.OnResultExecuted(filterContext);
        }
    }

    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new NoCacheForAuthenticatedFilter());
        }
    }
}
