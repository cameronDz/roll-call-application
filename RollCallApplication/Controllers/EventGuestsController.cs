using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
        public ActionResult AdminPasscodeCheck()
        {
            Trace.WriteLine("GET EventGuests/AdminPasscodeCheck");
            ViewBag.Title = "Passcode Check";
            ViewBag.Message = "Must enter passcode to view any list of guests and make edit/deletions" +
                "to guests that have checked in through application";
            return View();
        }

        [HttpPost]
        public ActionResult AdminPasscodeCheck(string password)
        {
            Trace.WriteLine("POST EventGuests/AdminPasscodeCheck");
            if (Settings.Default.AdminPasscode.Equals(password))
            {
                Session["rollCallApp-Authentication"] = Settings.Default.AdminPasscode;
                return RedirectToAction("IndexOfAllGuests");
            }
            ViewBag.Title = "Passcode Check";
            ViewBag.Message = "Enter passcode to view List/Edit/Delete pages.";
            ViewBag.EnteredWrongPasscode = true;
            return View();
        }

        // GET: EventGuests
        public ActionResult IndexOfAllGuests()
        {
            Trace.WriteLine("GET EventGuests/Index");
            ViewBag.Title = "Attendees";
            ViewBag.Message = "List of Attendees.";
            ViewBag.EventName = Settings.Default.EventName;
            return View(db.EventGuests.OrderByDescending(g => g.TimeOfCheckIn).ToList());
        }

        // GET: EventGuests
        [SimpleMembership]
        public ActionResult IndexOfCheckedInGuests()
        {
            Trace.WriteLine("GET EventGuests/Index");
            ViewBag.Title = "Attendees";
            ViewBag.Message = "List of Attendees.";
            ViewBag.TotalGuests = db.EventGuests.Count();
            ViewBag.TotalUniqueGuests = db.EventGuests.GroupBy(g => g.Email).Count();
            return View(db.EventGuests.OrderByDescending(g => g.TimeOfCheckIn).ToList());
        }

        // GET: EventGuests/Create
        public ActionResult RegisterGuest()
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
        public ActionResult RegisterGuest([Bind(Include = "GuestId,FirstName,LastName,Email")] EventGuest eventGuest)
        {
            Trace.WriteLine("POST EventGuest/Create");
            ViewBag.Title = "Check In";
            ViewBag.Message = "Checked In Page.";
            if (ModelState.IsValid)
            {
                eventGuest.TimeOfCheckIn = GetCurrentDateTimeWithOffSet();
                db.EventGuests.Add(eventGuest);
                db.SaveChanges();
                ModelState.Clear();
                ViewBag.SuccessfulCheckIn = true;
                return View(new EventGuest { });
            }
            ViewBag.FailedCheckIn = true;
            return View(eventGuest);
        }

        [SimpleMembership]
        public ActionResult LoadRegistrationList()
        {
            ViewBag.Title = "Load Registration List";
            ViewBag.Message = "Load Registration List Page";
            return View();
        }
        
        // GET: EventGuests/Edit/5
        public ActionResult Edit(int? id)
        {
            Trace.WriteLine("GET EventGuest/Edit");
            ViewBag.Title = "Check In Guest";
            ViewBag.Message = "Check In Guest Page.";
            if (id == null)
            {
                ViewBag.CouldNotFindGuest = true;
                return RedirectToAction("IndexOfAllGuests");
            }
            EventGuest eventGuest = db.EventGuests.Find(id);
            if (eventGuest == null)
            {
                ViewBag.CouldNotFindGuest = true;
                return RedirectToAction("IndexOfAllGuests");
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
            eventGuest.TimeOfCheckIn = GetCurrentDateTimeWithOffSet(); 
            if (ModelState.IsValid)
            {
                db.Entry(eventGuest).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("IndexOfAllGuests");
            }
            ViewBag.CouldNotEditGuest = true;
            ViewBag.Title = "Check In Guest";
            ViewBag.Message = "Check In Guest Page.";
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
                return RedirectToAction("IndexOfCheckedInGuests");
            }
            EventGuest eventGuest = db.EventGuests.Find(id);
            if (eventGuest == null)
            {
                ViewBag.CouldNotFindGuest = true;
                return RedirectToAction("IndexOfCheckedInGuests");
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
            return RedirectToAction("IndexOfCheckedInGuests");
        }

        /* Source: https://stackoverflow.com/questions/13337076/export-database-data-to-csv-from-view-by-date-range-asp-net-mvc3 */
        [HttpGet]
        public FileResult DownloadListAsCsv()
        {
            Trace.WriteLine("GET EventGuest/DownloadListAsCsv");
            IEnumerable<EventGuest> rows = db.EventGuests;

            StringBuilder csv = new StringBuilder();
            csv = WriteLineInCsvStringBuilder(csv, 
                "First Name", 
                "Last Name", 
                "Email", 
                "Preregistered", 
                "Check in Time");
            foreach (EventGuest row in rows)
            {
                WriteLineInCsvStringBuilder(csv, 
                    row.FirstName, 
                    row.LastName, 
                    row.Email, 
                    row.Preregistered.ToString(), 
                    row.TimeOfCheckIn.ToString());
            }

            var data = Encoding.UTF8.GetBytes(csv.ToString());
            string filename = "rollCallExtract-" + DateTime.Now.ToString() + ".csv";
            return File(data, "text/csv", filename);
        }

        private StringBuilder WriteLineInCsvStringBuilder(StringBuilder csv, String columnOne,
                String columnTwo, String columnThree, String columnFour, String columnFive)
        {
            csv.Append(columnOne).Append(',');
            csv.Append(columnTwo).Append(',');
            csv.Append(columnThree).Append(',');
            csv.Append(columnFour).Append(',');
            csv.Append(columnFive).Append(',');
            csv.AppendLine();
            return csv;
        }

        private DateTime GetCurrentDateTimeWithOffSet()
        {
            DateTime currentDateTime = DateTime.Now.ToUniversalTime();
            TimeSpan utcOffset = new TimeSpan(Settings.Default.TimeZoneOffsetHours, 0, 0);
            return currentDateTime.Subtract(utcOffset);
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
