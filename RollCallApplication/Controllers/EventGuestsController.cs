﻿using System;
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
            ViewBag.Message = "Enter passcode to view guests check in times and make " +
                "edit/deletions to guests.";
            return View();
        }

        [HttpPost]
        public ActionResult AdminPasscodeCheck(string password)
        {
            Trace.WriteLine("POST EventGuests/AdminPasscodeCheck");
            if (Settings.Default.AdminPasscode.Equals(password))
            {
                Session["rollCallApp-Authentication"] = Settings.Default.AdminPasscode;
                return RedirectToAction("AdministratorIndex");
            }
            ViewBag.Title = "Passcode Check";
            ViewBag.Message = "Must enter passcode to view any list of guests and make edit/deletions" +
                "to guests that have checked in through application";
            ViewBag.EnteredWrongPasscode = true;
            return View();
        }

        [SimpleMembership]
        public ActionResult AdministratorIndex()
        {
            Trace.WriteLine("GET EventGuests/AdministratorIndex");
            ViewBag.Title = "Administrator Index";
            return View();
        }

        public ActionResult PreregisteredCheckInList()
        {
            Trace.WriteLine("GET EventGuests/PreregisterCheckIn");
            ViewBag.Title = "Check In List";
            ViewBag.Message = "Preregister Guest Check In List.";
            ViewBag.EventName = Settings.Default.EventName;
            return View(db.EventGuests.OrderBy(g => g.LastName).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PreregisteredCheckInList(int? id)
        {
            Trace.WriteLine("POST EventGuests/PreregisterCheckIn");
            ViewBag.Title = "Check In List";
            ViewBag.Message = "Preregister Guest Check In List.";
            ViewBag.EventName = Settings.Default.EventName;
            if (id == null)
            {
                ViewBag.FailedCheckIn = true;
                return View(db.EventGuests.OrderBy(g => g.LastName).ToList());
            }
            EventGuest eventGuest = db.EventGuests.Find(id);
            if (eventGuest == null)
            {
                ViewBag.FailedCheckIn = true;
                return View(db.EventGuests.OrderBy(g => g.LastName).ToList());
            }
            eventGuest.TimeOfCheckIn = GetCurrentDateTimeWithOffSet();
            db.SaveChanges();
            ViewBag.SuccessfulCheckIn = true;
            return View(db.EventGuests.OrderBy(g => g.LastName).ToList());
        }

        public ActionResult RegisterGuest()
        {
            Trace.WriteLine("GET /EventGuests/RegisterGuest");
            ViewBag.Title = "Register Guest";
            ViewBag.Message = "Register or Check In an unregistered Guest for Event.";
            ViewBag.EventName = Settings.Default.EventName;
            return View(new EventGuest { });
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegisterGuest([Bind(Include = "GuestId,FirstName,LastName,Email,Preregistered")] EventGuest eventGuest)
        {
            Trace.WriteLine("POST /EventGuests/RegisterGuest");
            ViewBag.Title = "Register";
            ViewBag.Message = "Register or Check In an unregistered Guest for Event.";
            ViewBag.FailedCheckInPreregister = true;
            if (ModelState.IsValid)
            {
                if(eventGuest.Preregistered == false) eventGuest.TimeOfCheckIn = GetCurrentDateTimeWithOffSet();
                db.EventGuests.Add(eventGuest);
                db.SaveChanges();
                ModelState.Clear();
                if (eventGuest.Preregistered == false) ViewBag.SuccessfulCheckIn = true;
                else ViewBag.SuccessfulPreregister = true;
                ViewBag.FailedCheckInPreregister = null;
                return View(new EventGuest { });
            }
            return View(eventGuest);
        }

        [SimpleMembership]
        public ActionResult LoadRegistrationList()
        {
            Trace.WriteLine("GET EventGuests/LoadRegistrationList");
            ViewBag.Title = "Load Registration";
            ViewBag.Message = "Load Registration List through .csv file.";
            return View();
        }

        [SimpleMembership]
        public ActionResult GuestListIndex()
        {
            Trace.WriteLine("GET EventGuests/GuestListIndex");
            ViewBag.Title = "Guest List";
            ViewBag.Message = "List of all Event Guests.";
            ViewBag.TotalCheckedInGuests = db.EventGuests.Count(g => g.TimeOfCheckIn != null);
            return View(db.EventGuests.OrderByDescending(g => g.TimeOfCheckIn).ToList());
        }

        [SimpleMembership]
        public ActionResult EditGuest(int? id)
        {
            Trace.WriteLine("GET EventGuest/EditGuest/");
            ViewBag.Title = "Edit Guest";
            ViewBag.Message = "Make edits to a registered guest.";
            if (id == null)
            {
                //ViewBag.CouldNotFindGuest = true;
                return RedirectToAction("GuestListIndex");
            }
            EventGuest eventGuest = db.EventGuests.Find(id);
            if (eventGuest == null)
            {
                //ViewBag.CouldNotFindGuest = true;
                return RedirectToAction("GuestListIndex");
            }
            return View(eventGuest);
        }
        
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [SimpleMembership]
        [ValidateAntiForgeryToken]
        public ActionResult EditGuest([Bind(Include = "GuestId,FirstName,LastName,Email")] EventGuest eventGuest)
        {
            Trace.WriteLine("POST EventGuest/Edit/");
            if (ModelState.IsValid)
            {
                db.Entry(eventGuest).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("GuestListIndex");
            }
            //ViewBag.CouldNotEditGuest = true;
            ViewBag.Title = "Edit Guest";
            ViewBag.Message = "Make edits to a registered event guest.";
            return View(eventGuest);
        }

        [SimpleMembership]
        public ActionResult DeleteGuest(int? id)
        {
            Trace.WriteLine("GET EventGuest/Delete");
            ViewBag.Title = "Delete Guest";
            ViewBag.Message = "Are you sure you want to delete this event guest?";
            if (id == null)
            {
                //ViewBag.CouldNotFindGuest = true;
                return RedirectToAction("GuestListIndex");
            }
            EventGuest eventGuest = db.EventGuests.Find(id);
            if (eventGuest == null)
            {
                //ViewBag.CouldNotFindGuest = true;
                return RedirectToAction("GuestListIndex");
            }
            return View(eventGuest);
        }

        [HttpPost, ActionName("DeleteGuest")]
        [SimpleMembership]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Trace.WriteLine("POST EventGuest/DeleteGuest");
            EventGuest eventGuest = db.EventGuests.Find(id);
            db.EventGuests.Remove(eventGuest);
            db.SaveChanges();
            return RedirectToAction("GuestListIndex");
        }

        /* Source: https://stackoverflow.com/questions/13337076/export-database-data-to-csv-from-view-by-date-range-asp-net-mvc3 */
        [HttpGet]
        [SimpleMembership]
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
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
