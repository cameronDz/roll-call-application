using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using RollCallApplication.Attribute;
using RollCallApplication.DAL;
using RollCallApplication.Models;
using RollCallApplication.Properties;

namespace RollCallApplication.Controllers
{
    public class EventGuestsController : Controller
    {
        private RollCallContext db = new RollCallContext();

        // Passcode methods source: https://github.com/balexandre/Stackoverflow-Question-12378445 
        public ActionResult PasscodeCheck()
        {
            Trace.WriteLine("GET EventGuests/PasscodeCheck");
            ViewBag.Title = "Passcode Check";
            ViewBag.Message = "Enter passcode to view List/Edit/Delete pages.";
            return View();
        }

        [HttpPost]
        public ActionResult PasscodeCheck(string password)
        {
            Trace.WriteLine("POST EventGuests/PasscodeCheck");
            if (Settings.Default.IndexPasscode.Equals(password))
            {
                Session["rollCallApp-Authentication"] = Settings.Default.IndexPasscode;
                return RedirectToAction("Index");
            }
            ViewBag.Title = "Passcode Check";
            ViewBag.Message = "Enter passcode to view List/Edit/Delete pages.";
            ViewBag.EnteredWrongPasscode = true;
            return View();
        }

        // GET: EventGuests
        [SimpleMembership]
        public ActionResult Index()
        {
            Trace.WriteLine("GET EventGuests/Index");
            ViewBag.Title = "Attendees";
            ViewBag.Message = "List of Attendees.";
            ViewBag.TotalGuests = db.EventGuests.Count();
            ViewBag.TotalUniqueGuests = db.EventGuests.GroupBy(g => g.Email).Count();
            return View(db.EventGuests.OrderByDescending(g => g.TimeOfCheckIn).ToList());
        }
        
        // GET: EventGuests/Create
        public ActionResult Create()
        {
            Trace.WriteLine("GET /Home/Create");
            ViewBag.Title = "Check In";
            ViewBag.Message = "Checked in Page.";
            ViewBag.EventName = Settings.Default.EventName;
            return View(new EventGuest { });
        }

        // POST: EventGuests/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "GuestId,FirstName,LastName,Email")] EventGuest eventGuest)
        {
            Trace.WriteLine("POST EventGuest/Create");
            ViewBag.Title = "Check In";
            ViewBag.Message = "Checked In Page.";
            if (ModelState.IsValid)
            {
                eventGuest.TimeOfCheckIn = DateTime.Now.ToLocalTime();
                db.EventGuests.Add(eventGuest);
                db.SaveChanges();
                ModelState.Clear();
                ViewBag.SuccessfulCheckIn = true;
                return View(new EventGuest { });
            }
            ViewBag.FailedCheckIn = true;
            return View(eventGuest);
        }

        // GET: EventGuests/Edit/5
        [SimpleMembership]
        public ActionResult Edit(int? id)
        {
            Trace.WriteLine("GET EventGuest/Edit");
            ViewBag.Title = "Edit Guest";
            ViewBag.Message = "Edit Page.";
            if (id == null)
            {
                ViewBag.CouldNotFindGuest = true;
                return RedirectToAction("Index");
            }
            EventGuest eventGuest = db.EventGuests.Find(id);
            if (eventGuest == null)
            {
                ViewBag.CouldNotFindGuest = true;
                return RedirectToAction("Index");
            }
            return View(eventGuest);
        }

        // POST: EventGuests/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "GuestId,FirstName,LastName,Email")] EventGuest eventGuest)
        {
            Trace.WriteLine("POST EventGuest/Edit");
            eventGuest.TimeOfCheckIn = DateTime.Now.ToLocalTime();
            if (ModelState.IsValid)
            {
                db.Entry(eventGuest).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.CouldNotEditGuest = true;
            ViewBag.Title = "Edit Guest";
            ViewBag.Message = "Edit Page.";
            return View(eventGuest);
        }

        // GET: EventGuests/Delete/5
        [SimpleMembership]
        public ActionResult Delete(int? id)
        {
            Trace.WriteLine("GET EventGuest/Delete");
            ViewBag.Title = "Delete Guest";
            ViewBag.Message = "Delete Page.";
            if (id == null)
            {
                ViewBag.CouldNotFindGuest = true;
                return RedirectToAction("Index");
            }
            EventGuest eventGuest = db.EventGuests.Find(id);
            if (eventGuest == null)
            {
                ViewBag.CouldNotFindGuest = true;
                return RedirectToAction("Index");
            }
            return View(eventGuest);
        }

        // POST: EventGuests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Trace.WriteLine("POST EventGuest/Delete");
            EventGuest eventGuest = db.EventGuests.Find(id);
            db.EventGuests.Remove(eventGuest);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        /* Source: https://stackoverflow.com/questions/13337076/export-database-data-to-csv-from-view-by-date-range-asp-net-mvc3 */
        [HttpGet]
        public FileResult DownloadListAsCsv()
        {
            Trace.WriteLine("GET EventGuest/DownloadListAsCsv");
            IEnumerable<EventGuest> rows = db.EventGuests;

            StringBuilder csv = new StringBuilder();
            csv = WriteLineInCsvStringBuilder(csv, "First Name", "Last Name", "Email", "Check in Time");
            foreach (EventGuest row in rows)
            {
                WriteLineInCsvStringBuilder(csv, row.FirstName, row.LastName, row.Email, row.TimeOfCheckIn.ToString());
            }

            var data = Encoding.UTF8.GetBytes(csv.ToString());
            string filename = "rollCallExtract-" + DateTime.Now.ToString() + ".csv";
            return File(data, "text/csv", filename);
        }

        private StringBuilder WriteLineInCsvStringBuilder(StringBuilder csv, String columnOne,
                String columnTwo, String columnThree, String columnFour)
        {
            csv.Append(columnOne).Append(',');
            csv.Append(columnTwo).Append(',');
            csv.Append(columnThree).Append(',');
            csv.Append(columnFour).Append(',');
            csv.AppendLine();
            return csv;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
