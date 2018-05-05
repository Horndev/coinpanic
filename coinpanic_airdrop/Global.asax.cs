using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace coinpanic_airdrop
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            if (!Request.Url.Host.StartsWith("www") && !Request.Url.IsLoopback && !Request.Url.Host.StartsWith("coinpanicnode"))
            {
                UriBuilder builder = new UriBuilder(Request.Url);
                builder.Host = "www." + Request.Url.Host;
                Response.StatusCode = 301;
                Response.AddHeader("Location", builder.ToString());
                Response.End();
            }
        }

        //protected void Page_Load(object sender, EventArgs e)
        //{
        //    // check is secure connection used
        //    if (!Request.IsSecureConnection)
        //    {
        //        // redirect visitor to SSL connection
        //        Response.Redirect(Request.Url.AbsoluteUri.Replace("http://", "https://"));
        //    }
        //}
    }
}
