using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using System.Web.Caching;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace NodeInterface
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            //Keep alive
            _SetupRefreshJob();
        }

        private static void _SetupRefreshJob()
        {

            //remove a previous job
            Action remove = HttpContext.Current.Cache["Refresh"] as Action;
            if (remove is Action)
            {
                HttpContext.Current.Cache.Remove("Refresh");
                remove.EndInvoke(null);
            }

            //get the worker
            Action work = () => {
                while (true)
                {
                    Thread.Sleep(60000);
                    //TODO: Refresh Code (Explained in a moment)
                    WebClient refresh = new WebClient();
                    try
                    {
                        var coin = ConfigurationManager.AppSettings["ServiceCoin"];
                        refresh.UploadString("https://www.metabittrader.com/"+ coin +"/", string.Empty);
                    }
                    catch (Exception ex)
                    {
                        //snip...
                    }
                    finally
                    {
                        refresh.Dispose();
                    }
                }
            };
            work.BeginInvoke(null, null);

            //add this job to the cache
            HttpContext.Current.Cache.Add(
                "Refresh",
                work,
                null,
                Cache.NoAbsoluteExpiration,
                Cache.NoSlidingExpiration,
                CacheItemPriority.Normal,
                (s, o, r) => { _SetupRefreshJob(); }
                );
        }
    }

    
}
