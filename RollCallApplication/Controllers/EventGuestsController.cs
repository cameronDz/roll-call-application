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
        private RollCallContext db = new RollCallContext();
        private EventGuestRepository repository = new EventGuestRepository();
        private EventGuestService service = new EventGuestService();

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
            ViewBag.TotalCheckedInGuests = numberOfCheckInGuests();
            ViewBag.SortOrder = (String.IsNullOrEmpty(sortOrder) ? "last_name_ascd" : sortOrder);
            ViewBag.SearchParam = searchParam;
            return View(repository.GetOrderedEventGuestListWithSearchParam(sortOrder, searchParam));
        }

        public ActionResult PreregisteredCheckInList(String sortOrder, String searchParam)
        {
            Trace.WriteLine("GET EventGuests/PreregisterCheckIn sortOrder: " + sortOrder + ", searchParam: " + searchParam);
            ViewBag.Title = "Check In List";
            ViewBag.Message = EventGuestConstants.CHECK_IN_DEFAULT_MESSAGE;
            ViewBag.EventName = Settings.Default.EventName;
            ViewBag.SortOrder = (String.IsNullOrEmpty(sortOrder) ? "last_name_ascd" : sortOrder);
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
            EventGuest eventGuest = repository.GetEventGuestById(id);
            if (eventGuest == null) return View(repository.GetOrderedEventGuestListWithSearchParam("", ""));
            ViewBag.AlertMessage = EventGuestConstants.CHECK_IN_WELCOME_BACK;
            if (!repository.CheckInEventGuestToEvent(eventGuest)) return View(repository.GetOrderedEventGuestListWithSearchParam("", ""));
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
            if(emailAlreadyExistsForEventGuest(eventGuest.Email))
            {
                EventGuest existingGuest = db.EventGuests.FirstOrDefault(g => g.Email.Equals(eventGuest.Email));
                String errorMessage = generateExistingGuestErrorMessage(existingGuest);
                ModelState.AddModelError("Email", errorMessage);
                return View(eventGuest);
            }
            if(eventGuest.Preregistered == false) eventGuest.TimeOfCheckIn = service.GetCurrentDateTimeWithOffSet();
            addEventGuestToDbContext(eventGuest);
            ModelState.Clear();
            if (eventGuest.Preregistered == false) ViewBag.SuccessfulCheckIn = true;
            else ViewBag.SuccessfulPreregister = true;
            ViewBag.FailedCheckInPreregister = null;
            return View(new EventGuest { });
        }

        private Boolean emailAlreadyExistsForEventGuest(String email)
        {
            int count = db.EventGuests.Count(g => g.Email.Equals(email));
            return count > 0;
        }

        private String generateExistingGuestErrorMessage(EventGuest existingGuest)
        {
            String message = EventGuestConstants.REGISTRATION_EXISTING_ERROR + "\nFirst Name: " +
                existingGuest.FirstName + "\nLast Name: " + existingGuest.LastName + "\nEmail: " + existingGuest.Email;
            return message;
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
            ViewBag.Message = EventGuestConstants.LOAD_REGISTRANTS_DEFAULT;
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("File", EventGuestConstants.LOAD_REGISTRANTS_TABLE_ERROR);
                return View();
            }
            if (upload == null || upload.ContentLength <= 0)
            {
                ModelState.AddModelError("File", EventGuestConstants.LOAD_REGISTRANTS_UPLOAD_ERROR);
                return View();
            }
            if (!upload.FileName.EndsWith(".csv"))
            {
                ModelState.AddModelError("File", EventGuestConstants.LOAD_REGISTRANTS_FILE_TYPE_ERROR);
                return View();
            }
            DataTable csvTable = createNewDataTableFromUpload(upload);
            int[] firstLastEmailArray = createIntArrayForFirstLastEmailColumns(csvTable);
            if (intArrayHasNegativeValues(firstLastEmailArray))
            {
                ModelState.AddModelError("File", EventGuestConstants.LOAD_REGISTRANTS_MISSING_COLUMN_ERROR);
                return View();
            }
            int registeredCount = 0;
            int alreadyRegisteredCount = 0;
            foreach (DataRow row in csvTable.Rows)
            {
                EventGuest tableGuest = createEventGuestFromRowData(row, firstLastEmailArray);
                if (emailAlreadyExistsForEventGuest(tableGuest.Email)) alreadyRegisteredCount++;
                else if (String.IsNullOrEmpty(tableGuest.Email)) alreadyRegisteredCount++;
                else
                {
                    addEventGuestToDbContext(tableGuest);
                    registeredCount++;
                }
            }
            ViewBag.SuccessfulUploadMessage = true;
            ViewBag.RegisteredGuestCount = registeredCount;
            ViewBag.AlreadyRegisteredCount = alreadyRegisteredCount;
            return View(new DataTable());
        }
        
        private DataTable createNewDataTableFromUpload(HttpPostedFileBase upload)
        {
            Stream stream = upload.InputStream;
            DataTable csvTable = new DataTable();
            using (CsvReader csvReader =
                new CsvReader(new StreamReader(stream), true))
            {
                csvTable.Load(csvReader);
            }
            return csvTable;
        }

        private int[] createIntArrayForFirstLastEmailColumns(DataTable csvTable)
        {
            int[] array = { -1, -1, -1 };
            foreach (DataColumn col in csvTable.Columns)
            {
                if ("first name".Equals(col.ColumnName.ToLower()))
                    array[0] = csvTable.Columns.IndexOf(col);
                if ("last name".Equals(col.ColumnName.ToLower()))
                    array[1] = csvTable.Columns.IndexOf(col);
                if ("email".Equals(col.ColumnName.ToLower()))
                    array[2] = csvTable.Columns.IndexOf(col);
            }
            return array;
        }

        private Boolean intArrayHasNegativeValues(int[] array)
        {
            foreach(int i in array) if( i < 0) return true;
            return false;
        }

        private EventGuest createEventGuestFromRowData(DataRow row, int[] firstLastEmailArray)
        {
            EventGuest guest = new EventGuest();
            guest.FirstName = row.ItemArray.ElementAt(firstLastEmailArray[0]).ToString();
            guest.LastName = row.ItemArray.ElementAt(firstLastEmailArray[1]).ToString();
            guest.Email = row.ItemArray.ElementAt(firstLastEmailArray[2]).ToString();
            guest.Preregistered = true;
            String rowString = row.ItemArray.ElementAt(firstLastEmailArray[0]).ToString() + " " +
                row.ItemArray.ElementAt(firstLastEmailArray[1]).ToString() + " " + 
                row.ItemArray.ElementAt(firstLastEmailArray[2]).ToString();
            Trace.WriteLine(rowString);
            return guest;
        }

        private void addEventGuestToDbContext(EventGuest guest)
        {
            db.EventGuests.Add(guest);
            db.SaveChanges();
        }

        private int numberOfCheckInGuests()
        {
            return db.EventGuests.Count(g => g.TimeOfCheckIn != null);
        }

        [SimpleMembership]
        public ActionResult EditGuest(int? id)
        {
            Trace.WriteLine("GET EventGuest/EditGuest int: " + id);
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
            Trace.WriteLine("POST EventGuest/Edit eventGuest: " + eventGuest.ToString());
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
        public ActionResult UnCheckInGuest(int id)
        {
            EventGuest eventGuest = db.EventGuests.Find(id);
            if (eventGuest == null) return RedirectToAction("GuestListIndex");
            eventGuest.TimeOfCheckIn = null;
            db.Entry(eventGuest).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("GuestListIndex");
        }

        [SimpleMembership]
        public ActionResult InvertGuestExtraRaffleEntry(int id)
        {
            EventGuest eventGuest = db.EventGuests.Find(id);
            if (eventGuest == null) return RedirectToAction("GuestListIndex");
            eventGuest.ExtraRaffleEntry = !eventGuest.ExtraRaffleEntry;
            db.Entry(eventGuest).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("GuestListIndex");
        }

        [SimpleMembership]
        public ActionResult DeleteGuest(int? id)
        {
            Trace.WriteLine("GET EventGuest/Delete id: " + id);
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
            Trace.WriteLine("POST EventGuest/DeleteConfirmed id: " + id);
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
            byte[] csvData = service.FullEventGuestListInCsv();
            String fileName = service.CsvEventGuestFileName();
            return File(csvData, "text/csv", fileName);
        }

        [SimpleMembership]
        public ActionResult RandomSweepstakesPicker()
        {
            ViewBag.WinnerPicked = null;
            ViewBag.WinnerMessage = null;
            ViewBag.Title = "Sweepstakes Raffle!";
            ViewBag.Message = "Click the Red Button to run the sweep stakes!";
            return View();
        }

        [HttpPost]
        [SimpleMembership]
        public ActionResult RandomSweepstakesPicker(Boolean getRandomCheckedInGuest)
        {
            ViewBag.Title = "Sweepstakes Raffle!";
            List<EventGuest> checkedInList = db.EventGuests.Where(g => g.TimeOfCheckIn != null && g.WonCheckInRaffle == false).OrderBy(g => g.TimeOfCheckIn).ToList();
            List<EventGuest> extraRaffleList = db.EventGuests.Where(g => g.ExtraRaffleEntry == true && g.WonExtraRaffleEntry == false).OrderBy(g => g.Email).ToList();
            int numberOfRaffleEntrees = checkedInList.Count() + extraRaffleList.Count();
            if (numberOfRaffleEntrees < 1)
            {
                ViewBag.Message = "Unable to pick a winner since no one qualifies.";
                return View();
            }
            EventGuest winner = getWinningEventGuest(numberOfRaffleEntrees, checkedInList, extraRaffleList);
            ViewBag.Message = "YEEEEAAAAAA!!!!!";
            ViewBag.WinnerPicked = true;
            ViewBag.WinnerMessage = getCongratulationsMessage(winner);
            return View(winner);
        }

        private EventGuest getWinningEventGuest(int eligibilityCount, List<EventGuest> checkedInList ,List<EventGuest> extraRaffleList)
        {
            EventGuest winner;
            Random random = new Random();
            int randomIndex = random.Next(0, eligibilityCount);
            int overFlowIndex = randomIndex - checkedInList.Count();
            if (overFlowIndex >= 0)
            {
                winner = extraRaffleList.ElementAt(overFlowIndex);
                winner.WonExtraRaffleEntry = true;
            }
            else
            {
                winner = checkedInList.ElementAt(randomIndex);
                winner.WonCheckInRaffle = true;
            }
            db.Entry(winner).State = EntityState.Modified;
            db.SaveChanges();
            return winner;
        }

        private String getCongratulationsMessage(EventGuest guest)
        {
            return "Congratulations to " + guest.FirstName + " " + guest.LastName + "!";
        }

        private int getCheckInCount()
        {
            return db.EventGuests.Count(g => g.TimeOfCheckIn != null);
        }

        private List<EventGuest> listOfAllGuestsThatHaveACheckInTime()
        {
            List<EventGuest> guestList = db.EventGuests.ToList();
            List<EventGuest> checkInList = new List<EventGuest>();
            foreach (EventGuest g in guestList)
            {
                if (g.TimeOfCheckIn != null) checkInList.Add(g); 
            }
            return checkInList;
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
