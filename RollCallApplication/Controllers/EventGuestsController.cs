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
            Trace.WriteLine("POST EventGuests/AdminPasscodeCheck password: ***");
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

        public ActionResult PreregisteredCheckInList(String sortOrder, String searchParam)
        {
            Trace.WriteLine("GET EventGuests/PreregisterCheckIn sortOrder: " + sortOrder + ", searchParam: " + searchParam);
            ViewBag.Title = "Check In List";
            ViewBag.Message = "Preregister Guest Check In List.";
            ViewBag.EventName = Settings.Default.EventName;
            ViewBag.FirstNameSortParm = ("first_name_ascd").Equals(sortOrder) ? "first_name_desc" : "first_name_ascd";
            ViewBag.LastNameSortParm = (("last_name_ascd").Equals(sortOrder) || String.IsNullOrEmpty(sortOrder)) ? "last_name_desc" : "last_name_ascd";
            ViewBag.EmailSortParm = ("email_ascd").Equals(sortOrder) ? "email_desc" : "email_ascd";
            ViewBag.SortOrder = sortOrder;
            ViewBag.SearchParam = searchParam;
            List<EventGuest> guestList = getEventGuestsWithSearchParam(searchParam);
            return View(orderListOfGuests(sortOrder, guestList));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PreregisteredCheckInList(int? id)
        {
            Trace.WriteLine("POST EventGuests/PreregisterCheckIn id: " + id);
            ViewBag.Title = "Check In List";
            ViewBag.Message = "Preregister Guest Check In List.";
            ViewBag.EventName = Settings.Default.EventName;
            ViewBag.FirstNameSortParm = "first_name_ascd";
            ViewBag.LastNameSortParm = "last_name_desc";
            ViewBag.EmailSortParm = "email_ascd";
            ViewBag.SortOrder = "";
            ViewBag.SearchParam = "";
            List<EventGuest> fullList = orderListOfGuests("", getEventGuestsWithSearchParam(""));
            if (id == null)
            {
                ViewBag.FailedCheckIn = true;
                return View(fullList);
            }
            EventGuest eventGuest = db.EventGuests.Find(id);
            if (eventGuest == null)
            {
                ViewBag.FailedCheckIn = true;
                return View(fullList);
            }
            eventGuest.TimeOfCheckIn = GetCurrentDateTimeWithOffSet();
            db.SaveChanges();
            ViewBag.SuccessfulCheckIn = true;
            return View(fullList);
        }
        
        private List<EventGuest> getEventGuestsWithSearchParam(String searchParam)
        {
            if (String.IsNullOrEmpty(searchParam)) searchParam = "";
            else searchParam = searchParam.ToLower();
            List<EventGuest> fullGuestList = db.EventGuests.ToList();
            if (String.IsNullOrEmpty(searchParam)) return fullGuestList;
            List<EventGuest> limitedGuestList = new List<EventGuest>();
            foreach(EventGuest guest in fullGuestList) {
                if (guest.FirstName.ToLower().StartsWith(searchParam)) limitedGuestList.Add(guest);
                else if (guest.LastName.ToLower().StartsWith(searchParam)) limitedGuestList.Add(guest);
                else if (guest.Email.ToLower().StartsWith(searchParam)) limitedGuestList.Add(guest);
            }
            return limitedGuestList;
        }

        private List<EventGuest> orderListOfGuests(String sortOrder, List<EventGuest> list)
        {
            if (String.IsNullOrEmpty(sortOrder)) sortOrder = "";
            switch (sortOrder)
            {
                case "first_name_desc":
                    return list.OrderByDescending(g => g.FirstName).ToList(); 
                case "first_name_ascd":
                    return list.OrderBy(g => g.FirstName).ToList();
                case "last_name_desc":
                    return list.OrderByDescending(g => g.LastName).ToList();
                case "last_name_ascd":
                    return list.OrderBy(g => g.LastName).ToList();
                case "email_desc":
                    return list.OrderByDescending(g => g.Email).ToList();
                case "email_ascd":
                    return list.OrderBy(g => g.Email).ToList();
                default:
                    return list.OrderBy(g => g.LastName).ToList();
            }
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
            Trace.WriteLine("POST /EventGuests/RegisterGuest eventGuest: " + eventGuest.ToString());
            ViewBag.Title = "Register";
            ViewBag.Message = "Register or Check In an unregistered Guest for Event.";
            ViewBag.EventName = Settings.Default.EventName;
            ViewBag.FailedCheckInPreregister = true;
            if (ModelState.IsValid)
            {
                if(emailAlreadyExistsForEventGuest(eventGuest.Email))
                {
                    EventGuest existingGuest = db.EventGuests.FirstOrDefault(g => g.Email.Equals(eventGuest.Email));
                    String errorMessage = generateExistingGuestErrorMessage(existingGuest);
                    ModelState.AddModelError("Email", errorMessage);
                    return View(eventGuest);
                }
                if(eventGuest.Preregistered == false) eventGuest.TimeOfCheckIn = GetCurrentDateTimeWithOffSet();
                addEventGuestToDbContext(eventGuest);
                ModelState.Clear();
                if (eventGuest.Preregistered == false) ViewBag.SuccessfulCheckIn = true;
                else ViewBag.SuccessfulPreregister = true;
                ViewBag.FailedCheckInPreregister = null;
                return View(new EventGuest { });
            }
            return View(eventGuest);
        }

        private Boolean emailAlreadyExistsForEventGuest(String email)
        {
            int count = db.EventGuests.Count(g => g.Email.Equals(email));
            return count > 0;
        }

        private String generateExistingGuestErrorMessage(EventGuest existingGuest)
        {
            String message = "Failed Registration. Email already registered on database under.\nFirst Name: " +
                existingGuest.FirstName + "\nLast Name: " + existingGuest.LastName + "\nEmail: " + existingGuest.Email;
            return message;
        }

        [SimpleMembership]
        public ActionResult LoadRegistrationList()
        {
            Trace.WriteLine("GET EventGuests/LoadRegistrationList");
            ViewBag.Title = "Load Registration";
            ViewBag.Message = "Load Registration List through .csv file.";
            return View();
        }

        [HttpPost]
        [SimpleMembership]
        [ValidateAntiForgeryToken]
        public ActionResult LoadRegistrationList(HttpPostedFileBase upload)
        {
            Trace.WriteLine("POST EventGuests/LoadRegistrationList upload: " + upload.ToString());
            ViewBag.Title = "Load Registration";
            ViewBag.Message = "Load Registration List through .csv file.";
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("File", "Failed Upload. Error on in table.");
                return View();
            }
            if (upload == null || upload.ContentLength <= 0)
            {
                ModelState.AddModelError("File", "Failed Upload. Upload never arrived.");
                return View();
            }
            if (!upload.FileName.EndsWith(".csv"))
            {
                String errorMessage = "Failed Upload. Unable to load registrants due to upload not being .csv file.";
                ModelState.AddModelError("File", errorMessage);
                return View();
            }
            DataTable csvTable = createNewDataTableFromUpload(upload);
            int[] firstLastEmailArray = createIntArrayForFirstLastEmailColumns(csvTable);
            if (intArrayHasNegativeValues(firstLastEmailArray))
            {
                String errorMessage = "Failed Upload. Unable to load registrants due to column name issue is .csv file.";
                ModelState.AddModelError("File", errorMessage);
                return View();
            }
            int count = 0;
            foreach (DataRow row in csvTable.Rows)
            {
                EventGuest tableGuest = createEventGuestFromRowData(row, firstLastEmailArray);
                if (emailAlreadyExistsForEventGuest(tableGuest.Email)) { } //do not add guest 
                else
                {
                    addEventGuestToDbContext(tableGuest);
                    count++;
                }
            }
            ViewBag.SuccessfulUploadMessage = true;
            ViewBag.RegisteredGuestCount = count;
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

        [SimpleMembership]
        public ActionResult GuestListIndex()
        {
            Trace.WriteLine("GET EventGuests/GuestListIndex");
            ViewBag.Title = "Guest List";
            ViewBag.Message = "List of all Event Guests.";
            ViewBag.TotalCheckedInGuests = numberOfCheckInGuests();
            return View(db.EventGuests.OrderByDescending(g => g.TimeOfCheckIn).ToList());
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
            Random random = new Random();
            int randomIndex = random.Next(0, numberOfCheckInGuests());
            List<EventGuest> list = db.EventGuests.Where(g => g.TimeOfCheckIn != null).ToList();
            EventGuest winner = list.ElementAt(randomIndex);
            ViewBag.Title = "Sweepstakes Raffle!";
            ViewBag.Message = "YEEEEAAAAAA!!!!!";
            ViewBag.WinnerPicked = true;
            ViewBag.WinnerMessage = getCongratulationsMessage(winner);
            return View(winner);
        }

        private String getCongratulationsMessage(EventGuest guest)
        {
            return "Congratulations to " + guest.FirstName + " " + guest.LastName + "!";
        }

        [SimpleMembership]
        public ActionResult GuessCheckInTime(int? randomNumber)
        {
            Trace.WriteLine("GET EventGuests/GuessCheckInTime randomNumber: " + randomNumber);
            ViewBag.Title = "Guess Check In Time";
            if (listOfAllGuestsThatHaveACheckInTime().Count() < 1)
            {
                ViewBag.Message = "No checked in guests. Can not guess a time.";
                return View();
            }
            String instructionMessage = "Enter a number between 1 and " + getCheckInCount() + "!";
            ViewBag.Message = instructionMessage;
            if(randomNumber == null)
            {
                return View();
            }
            ViewBag.Message = "Make sure the number you guess is in the ranged..\n" + instructionMessage;
            if (randomNumber > getCheckInCount() || randomNumber < 1)  return View();
            try
            {
                int nonNullNumber = randomNumber ?? default(int);
                EventGuest guest = listOfAllGuestsThatHaveACheckInTime().ElementAt(nonNullNumber-1);
                ViewBag.Message = instructionMessage;
                ViewBag.Congrats = true;
                ViewBag.CheckInGuess = randomNumber;
                ViewBag.CongratulationsMessage = getCongratulationsMessage(guest);
                return View(guest);
            }
            catch (ArgumentOutOfRangeException e)
            {
                Trace.WriteLine(e);
                return View();
            }
            catch (ArgumentNullException e) 
            {
                Trace.WriteLine(e);
                return View();
            }
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
                if (g.TimeOfCheckIn != null)
                {
                    checkInList.Add(g);
                }
            }
            return checkInList;
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
