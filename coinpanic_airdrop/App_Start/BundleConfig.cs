using System.Web;
using System.Web.Optimization;

namespace coinpanic_airdrop
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/signalr").Include(
                        "~/Scripts/jquery.signalR-2.2.3.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/moment").Include(
                        "~/Scripts/moment.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/sparkline").Include(
                        "~/Scripts/jquery.sparkline.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/datatables").Include(
                        "~/Scripts/DataTables/datatables.min.js"));                 // Included

            bundles.Add(new ScriptBundle("~/bundles/jquery/unobtrusive").Include(
                        "~/Scripts/jquery.unobtrusive-ajax.min.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/bower_components/jQuery/dist/jquery.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/icons").Include(
                        "~/bower_components/webicon/jquery-webicon.min.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at https://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                        "~/bower_components/popper.js/dist/umd/popper.min.js",      // Included
                        "~/bower_components/bootstrap/dist/js/bootstrap.min.js",    // Included
                        "~/Scripts/respond.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                        "~/bower_components/bootstrap/dist/css/bootstrap.min.css",  // Included
                        "~/Content/site.css"));

            bundles.Add(new StyleBundle("~/bundles/fontawesome/css").Include(
                      "~/bower_components/font-awesome/css/font-awesome.min.css", new CssRewriteUrlTransform()));   // Included

            bundles.Add(new ScriptBundle("~/bundles/datatables/css").Include(
                       "~/Scripts/DataTables/datatables.min.css"));                 // Included
        }
    }
}
