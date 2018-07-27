using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using LumenWorks.Framework.IO.Csv;
using RollCallApplication.Attribute;
using RollCallApplication.Constants;
using RollCallApplication.DAL;
using RollCallApplication.Models;
using RollCallApplication.Properties;
using RollCallApplication.Repositories;
using RollCallApplication.Services;

namespace RollCallApplication.Controllers
{
    public class EventGuestsController : Controller
    {
        private EventGuestRepository repository;
        private EventGuestService service;

        public EventGuestsController()
        {
            repository = new EventGuestRepository();
            service = new EventGuestService(repository);
        }

        public EventGuestsController(EventGuestRepository repository)
        {
            this.repository = repository;
            service = new EventGuestService(repository);
        }
        // Passcode methods source: https://github.com/balexandre/Stackoverflow-Question-12378445 
        public ActionResult AdminPasscodeCheck()
        {
            Trace.WriteLine("GET EventGuests/AdminPasscodeCheck");
            ViewBag.Title = "Passcode Check";
            ViewBag.Message = EventGuestConstants.ADMIN_PASSCODE_DEFAULT;
            return View();
        }
        [HttpPost]
        public ActionResult AdminPasscodeCheck(String password)
        {
            Trace.WriteLine("POST EventGuests/AdminPasscodeCheck password: ***");
            if (Settings.Default.AdminPasscode.Equals(password))
            {
                Session["rollCallApp-Authentication"] = Settings.Default.AdminPasscode;
                return RedirectToAction("AdministratorIndex");
            }
            ViewBag.Title = "Passcode Check";
            ViewBag.Message = EventGuestConstants.ADMIN_PASSCODE_WARNING;
            ViewBag.EnteredWrongPasscode = true;
            return View();
        }
        [SimpleMembership]
        public ActionResult AdministratorIndex()
        {
            Trace.WriteLine("GET EventGuests/AdministratorIndex");
            ViewBag.Title = "Administrator Index";
            ViewBag.Message = EventGuestConstants.ADMIN_PASSCODE_DEFAULT;
            return View();
        }
        [SimpleMembership]
        public ActionResult GuestListIndex(String sortOrder, String searchParam)
        {
            Trace.WriteLine("GET EventGuests/GuestListIndex");
            ViewBag.Title = "Guest List";
            ViewBag.Message = "List of all Event Guests.";
            ViewBag.TotalCheckedInGuests = repository.GetNumberOfCheckedInGuests();
            ViewBag.SortOrder = service.GetCurrentOrDefaultSortOrder(sortOrder);
            ViewBag.SearchParam = searchParam;
            return View(repository.GetOrderedEventGuestListWithSearchParam(sortOrder, searchParam));
        }
        public ActionResult PreregisteredCheckInList(String sortOrder, String searchParam)
        {
            Trace.WriteLine("GET EventGuests/PreregisterCheckIn sortOrder: " + sortOrder + ", searchParam: " + searchParam);
            ViewBag.Title = "Check In List";
            ViewBag.Message = EventGuestConstants.CHECK_IN_DEFAULT_MESSAGE;
            ViewBag.EventName = Settings.Default.EventName;
            ViewBag.SortOrder = service.GetCurrentOrDefaultSortOrder(sortOrder);
            ViewBag.SearchParam = searchParam;
            return View(repository.GetOrderedEventGuestListWithSearchParam(sortOrder, searchParam));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PreregisteredCheckInList(int? id)
        {
            Trace.WriteLine("POST EventGuests/PreregisterCheckIn id: " + id);
            ViewBag.Title = "Check In List";
            ViewBag.Message = EventGuestConstants.CHECK_IN_DEFAULT_MESSAGE; 
            ViewBag.EventName = Settings.Default.EventName;
            ViewBag.SortOrder = "";
            ViewBag.SearchParam = "";
            ViewBag.CheckInAttemptMade = true;
            ViewBag.AlertMessage = EventGuestConstants.CHECK_IN_FAIL;
            if (repository.GetEventGuestById(id) == null) return View(repository.GetOrderedEventGuestListWithSearchParam("", ""));
            ViewBag.AlertMessage = EventGuestConstants.CHECK_IN_WELCOME_BACK;
            if (!repository.CheckInEventGuestToEvent(id)) return View(repository.GetOrderedEventGuestListWithSearchParam("", ""));
            ViewBag.AlertMessage = EventGuestConstants.CHECK_IN_SUCCESS; 
            return View(repository.GetOrderedEventGuestListWithSearchParam("", ""));
        }

        public ActionResult RegisterGuest()
        {
            Trace.WriteLine("GET /EventGuests/RegisterGuest");
            ViewBag.Title = "Register Guest";
            ViewBag.Message = EventGuestConstants.REGISTRATION_DEFAULT;
            ViewBag.EventName = Settings.Default.EventName;
            return View(new EventGuest { });
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegisterGuest([Bind(Include = "GuestId,FirstName,LastName,Email,Preregistered,WonCheckInRaffle,ExtraRaffleEntry,WonExtraRaffleEntry")] EventGuest eventGuest)
        {
            Trace.WriteLine("POST /EventGuests/RegisterGuest eventGuest: " + eventGuest.ToString());
            ViewBag.Title = "Register";
            ViewBag.Message = EventGuestConstants.REGISTRATION_DEFAULT;
            ViewBag.EventName = Settings.Default.EventName;
            ViewBag.FailedCheckInPreregister = true;
            if (!ModelState.IsValid) return View(eventGuest);
            if(!repository.AddEventGuestToDbContext(eventGuest))
            {
                String errorMessage = ViewBag.Message = EventGuestConstants.REGISTRATION_EXISTING_ERROR + eventGuest.Email; 
                return View(eventGuest);
            }
            if (eventGuest.Preregistered == false) ViewBag.SuccessfulCheckIn = true;
            else ViewBag.SuccessfulPreregister = true;
            ViewBag.FailedCheckInPreregister = null;
            return View(new EventGuest { });
        }

        [SimpleMembership]
        public ActionResult LoadRegistrationList()
        {
            Trace.WriteLine("GET EventGuests/LoadRegistrationList");
            ViewBag.Title = "Load Registration";
            ViewBag.Message = EventGuestConstants.LOAD_REGISTRANTS_DEFAULT;
            return View();
        }
        [HttpPost]
        [SimpleMembership]
        [ValidateAntiForgeryToken]
        public ActionResult LoadRegistrationList(HttpPostedFileBase upload)
        {
            Trace.WriteLine("POST EventGuests/LoadRegistrationList upload: " + upload.ToString());
            ViewBag.Title = "Load Registration";
            CsvUploadStatus csvUploadStatus = service.UploadGuestsFromCsvFile(upload);
            if(!csvUploadStatus.SuccessfulUpload)
            {
                ViewBag.Message = csvUploadStatus.ErrorMessage;
                return View(new DataTable());
            }
            ViewBag.SuccessfulUploadMessage = true;
            ViewBag.RegisteredGuestCount = csvUploadStatus.EventGuestsUploaded;
            ViewBag.AlreadyRegisteredCount = csvUploadStatus.ColumnsNotUploaded;
            return View(new DataTable());
        }
        [SimpleMembership]
        public ActionResult EditGuest(int? id)
        {
            Trace.WriteLine("GET EventGuest/EditGuest int: " + id);
            ViewBag.Title = "Edit Guest";
            ViewBag.Message = "Make edits to a registered guest.";
            EventGuest eventGuest = repository.GetEventGuestById(id); 
            if (eventGuest == null) return RedirectToAction("GuestListIndex");
            return View(eventGuest);
        }
        
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [SimpleMembership]
        [ValidateAntiForgeryToken]
        public ActionResult EditGuest([Bind(Include = "GuestId,FirstName,LastName,Email")] EventGuest eventGuest)
        {
            Trace.WriteLine("POST EventGuest/Edit eventGuest: " + eventGuest.ToString());
            ViewBag.Title = "Edit Guest";
            ViewBag.Message = "Make edits to a registered event guest.";
            if (ModelState.IsValid)
            {
                repository.UpdateEventGuest(eventGuest);
                return RedirectToAction("GuestListIndex");
            }
            return View(eventGuest);
        }

        [SimpleMembership]
        public ActionResult UnCheckInGuest(int id)
        {
            repository.UncheckInEventGuest(id);
            return RedirectToAction("GuestListIndex");
        }
        [SimpleMembership]
        public ActionResult DeleteGuest(int? id)
        {
            Trace.WriteLine("GET EventGuest/Delete id: " + id);
            ViewBag.Title = "Delete Guest";
            ViewBag.Message = "Are you sure you want to delete this event guest?";
            EventGuest eventGuest = repository.GetEventGuestById(id); 
            if (eventGuest == null) return RedirectToAction("GuestListIndex");
            return View(eventGuest);
        }
        [HttpPost, ActionName("DeleteGuest")]
        [SimpleMembership]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Trace.WriteLine("POST EventGuest/DeleteConfirmed id: " + id);
            repository.DeleteEventGuestById(id);
            return RedirectToAction("GuestListIndex");
        }
        /* Source: https://stackoverflow.com/questions/13337076/export-database-data-to-csv-from-view-by-date-range-asp-net-mvc3 */
        [HttpGet]
        [SimpleMembership]
        public FileResult DownloadListAsCsv()
        {
            Trace.WriteLine("GET EventGuest/DownloadListAsCsv");
            byte[] csvData = service.FullEventGuestListInCsv();
            String fileName = service.CsvEventGuestFileName();
            return File(csvData, "text/csv", fileName);
        }

        [SimpleMembership]
        public ActionResult InvertGuestExtraRaffleEntry(int id)
        {
            return RedirectToAction("AdministratorIndex");
            //EventGuest eventGuest = db.EventGuests.Find(id);
            //if (eventGuest == null) return RedirectToAction("GuestListIndex");
            //eventGuest.ExtraRaffleEntry = !eventGuest.ExtraRaffleEntry;
            //db.Entry(eventGuest).State = EntityState.Modified;
            //db.SaveChanges();
            //return RedirectToAction("GuestListIndex");
        }
        [SimpleMembership]
        public ActionResult RandomSweepstakesPicker()
        {
            return RedirectToAction("AdministratorIndex");
            //ViewBag.WinnerPicked = null;
            //ViewBag.WinnerMessage = null;
            //ViewBag.Title = "Sweepstakes Raffle!";
            //ViewBag.Message = "Click the Red Button to run the sweep stakes!";
            //return View();
        }
        [HttpPost]
        [SimpleMembership]
        public ActionResult RandomSweepstakesPicker(Boolean getRandomCheckedInGuest)
        {
            return RedirectToAction("AdministratorIndex");
            //ViewBag.Title = "Sweepstakes Raffle!";
            //List<EventGuest> checkedInList = db.EventGuests.Where(g => g.TimeOfCheckIn != null && g.WonCheckInRaffle == false).OrderBy(g => g.TimeOfCheckIn).ToList();
            //List<EventGuest> extraRaffleList = db.EventGuests.Where(g => g.ExtraRaffleEntry == true && g.WonExtraRaffleEntry == false).OrderBy(g => g.Email).ToList();
            //int numberOfRaffleEntrees = checkedInList.Count() + extraRaffleList.Count();
            //if (numberOfRaffleEntrees < 1)
            //{
            //    ViewBag.Message = "Unable to pick a winner since no one qualifies.";
            //    return View();
            //}
            //EventGuest winner = getWinningEventGuest(numberOfRaffleEntrees, checkedInList, extraRaffleList);
            //ViewBag.Message = "YEEEEAAAAAA!!!!!";
            //ViewBag.WinnerPicked = true;
            //ViewBag.WinnerMessage = getCongratulationsMessage(winner);
            //return View(winner);
        }
        //private EventGuest getWinningEventGuest(int eligibilityCount, List<EventGuest> checkedInList ,List<EventGuest> extraRaffleList)
        //{
        //    EventGuest winner;
        //    Random random = new Random();
        //    int randomIndex = random.Next(0, eligibilityCount);
        //    int overFlowIndex = randomIndex - checkedInList.Count();
        //    if (overFlowIndex >= 0)
        //    {
        //        winner = extraRaffleList.ElementAt(overFlowIndex);
        //        winner.WonExtraRaffleEntry = true;
        //    }
        //    else
        //    {
        //        winner = checkedInList.ElementAt(randomIndex);
        //        winner.WonCheckInRaffle = true;
        //    }
        //    db.Entry(winner).State = EntityState.Modified;
        //    db.SaveChanges();
        //    return winner;
        //}
        //private String getCongratulationsMessage(EventGuest guest)
        //{
        //    return "Congratulations to " + guest.FirstName + " " + guest.LastName + "!";
        //}
    }
}
