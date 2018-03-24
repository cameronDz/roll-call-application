using RollCallApplication.Properties;
using System.Web.Mvc;

namespace RollCallApplication.Attribute
{
    // Source: https://github.com/balexandre/Stackoverflow-Question-12378445 
    public class SimpleMembershipAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Settings.Default.LockIndexFlagTurnedOn)
            {
                //redirect if not authenticated
                if (filterContext.HttpContext.Session["rollCallApp-Authentication"] == null ||
                    !((Settings.Default.IndexPasscode).Equals(
                        filterContext.HttpContext.Session["rollCallApp-Authentication"])))
                {
                    //use the current url for the redirect
                    string redirectOnSuccess = filterContext.HttpContext.Request.Url.AbsolutePath;

                    //send them off to the login page
                    string redirectUrl = string.Format("?ReturnUrl={0}", redirectOnSuccess);
                    string loginUrl = "/EventGuests/PasscodeCheck" + redirectUrl;
                    filterContext.HttpContext.Response.Redirect(loginUrl, true);
                }
            }
        }
    }
}