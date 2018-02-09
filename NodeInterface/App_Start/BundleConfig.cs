using System.Web;
using System.Web.Optimization;

namespace NodeInterface
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/bower_components/jQuery/dist/jquery.js"));

            // Jquery validator & unobstrusive ajax  
            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                "~/bower_components/jquery-ajax-unobtrusive/jquery.unobtrusive-ajax.js",
                "~/bower_components/jquery-ajax-unobtrusive/jquery.unobtrusive-ajax.min.js",
                "~/bower_components/jquery-validation/jquery.validate.js",
                "~/bower_components/jquery-validation/jquery.validate.unobtrusive.js",
                "~/bower_components/jquery-validation/jquery.validate.unobtrusive.min.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at https://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            // Custom Form  
            bundles.Add(new ScriptBundle("~/bundles/broadcast").Include(
                    "~/Scripts/broadcast.js"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/bower_components/bootstrap/dist/js/bootstrap.min.js",
                      "~/Scripts/respond.js"));

            bundles.Add(new StyleBundle("~/bundles/fontawesome/css").Include(
                      "~/bower_components/font-awesome/css/font-awesome.min.css", new CssRewriteUrlTransform()));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/site.css"));


        }
    }
}
