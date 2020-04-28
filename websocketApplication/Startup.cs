using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(websocketApplication.Startup))]
namespace websocketApplication
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
