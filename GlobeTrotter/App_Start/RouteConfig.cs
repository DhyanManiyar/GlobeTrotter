using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace GlobeTrotter
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "UserTrip",
                url: "User/trip",
                defaults: new { controller = "User", action = "Trip" }
            );

            routes.MapRoute(
                name: "UserTrips",
                url: "User/trips",
                defaults: new { controller = "User", action = "Trip" }
            );

            routes.MapRoute(
                name: "UserDashboard",
                url: "User",
                defaults: new { controller = "User", action = "Index" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
