using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(GlobeTrotter.Startup))]
namespace GlobeTrotter
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
