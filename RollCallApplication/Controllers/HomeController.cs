using RollCallApplication.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RollCallApplication.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Home";
            ViewBag.Message = "Application Home Page.";
            ViewBag.GitHubRepositoryAddress = Settings.Default.GitHubRepositoryAddress;
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Title = "About";
            ViewBag.Message = "Application About Page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Title = "Contact";
            ViewBag.Message = "Application Contact Page.";
            ViewBag.CreatorContactEmail = Settings.Default.CreatorEmail;
            ViewBag.CreatorContactEmailLink = "mailto:" + Settings.Default.CreatorEmail;
            ViewBag.GitHubRepositoryAddress = Settings.Default.GitHubRepositoryAddress;
            return View();
        }
    }
}