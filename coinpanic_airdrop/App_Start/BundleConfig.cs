using System.Web;
using System.Web.Optimization;

namespace coinpanic_airdrop
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/bower_components/jQuery/dist/jquery.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/bower_components/jquery-validation/jquery.validate.js"));

            bundles.Add(new ScriptBundle("~/bundles/icons").Include(
                        "~/bower_components/webicon/jquery-webicon.min.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at https://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/bower_components/bootstrap/dist/js/bootstrap.min.js",
                      "~/Scripts/respond.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/bower_components/bootstrap/dist/css/bootstrap.min.css",
                      "~/Content/site.css"));

            bundles.Add(new StyleBundle("~/bundles/fontawesome/css").Include(
                      "~/bower_components/font-awesome/css/font-awesome.min.css", new CssRewriteUrlTransform()));

            bundles.Add(new ScriptBundle("~/bundles/cryptolib").Include(
                       "~/bower_components/bitcore-lib/bitcore-lib.js"));
        }
    }
}
