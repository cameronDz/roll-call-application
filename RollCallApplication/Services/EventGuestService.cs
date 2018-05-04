using LumenWorks.Framework.IO.Csv;
using RollCallApplication.Constants;
using RollCallApplication.DAL;
using RollCallApplication.Models;
using RollCallApplication.Properties;
using RollCallApplication.Repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace RollCallApplication.Services
{
    public class EventGuestService
    {
        private EventGuestRepository repository; 
        public EventGuestService(EventGuestRepository repository)
        {
            this.repository = repository;
        }
        public byte[] FullEventGuestListInCsv()
        {
            IEnumerable<EventGuest> rows = repository.GetOrderedEventGuestListWithSearchParam("", "");
            StringBuilder csv = new StringBuilder();
            csv = WriteLineInCsvStringBuilder(csv,
                    "First Name",
                    "Last Name",
                    "Email",
                    "Preregistered",
                    "Check in Time",
                    "Won Check In Raffle",
                    "Extra Raffle Entry",
                    "Won Extra Raffle Entry");
            foreach (EventGuest row in rows)
            {
                WriteLineInCsvStringBuilder(csv,
                    row.FirstName,
                    row.LastName,
                    row.Email,
                    row.Preregistered.ToString(),
                    row.TimeOfCheckIn.ToString(),
                    row.WonCheckInRaffle.ToString(),
                    row.ExtraRaffleEntry.ToString(),
                    row.WonExtraRaffleEntry.ToString());
            }
            var data = Encoding.UTF8.GetBytes(csv.ToString());
            return data; 
        }
        public String GetCurrentOrDefaultSortOrder(String sortOrder)
        {
            return (String.IsNullOrEmpty(sortOrder) ? "last_name_ascd" : sortOrder);
        }
        public String CsvEventGuestFileName()
        {
            return "rollCallExtract-" + GetCurrentDateTimeWithOffSet().ToString() + ".csv";
        }
        public DateTime GetCurrentDateTimeWithOffSet()
        {
            DateTime currentDateTime = DateTime.Now.ToUniversalTime();
            TimeSpan utcOffset = new TimeSpan(Settings.Default.TimeZoneOffsetHours, 0, 0);
            return currentDateTime.Subtract(utcOffset);
        }
        private StringBuilder WriteLineInCsvStringBuilder(StringBuilder csv, 
            String columnOne, String columnTwo, String columnThree, String columnFour, 
            String columnFive, String columnSix, String columnSeven, String columnEight)
        {
            csv.Append(columnOne).Append(',');
            csv.Append(columnTwo).Append(',');
            csv.Append(columnThree).Append(',');
            csv.Append(columnFour).Append(',');
            csv.Append(columnFive).Append(',');
            csv.Append(columnSix).Append(',');
            csv.Append(columnSeven).Append(',');
            csv.Append(columnEight).Append(',');
            csv.AppendLine();
            return csv;
        }
        public CsvUploadStatus UploadGuestsFromCsvFile(HttpPostedFileBase upload)
        {
            CsvUploadStatus status = new CsvUploadStatus();
            status.SuccessfulUpload = false;
            status.ErrorMessage = EventGuestConstants.LOAD_REGISTRANTS_UPLOAD_ERROR;
            if (upload == null || upload.ContentLength <= 0) return status;
            status.ErrorMessage = EventGuestConstants.LOAD_REGISTRANTS_FILE_TYPE_ERROR;
            if (!upload.FileName.EndsWith(".csv")) return status;
            DataTable csvTable = createNewDataTableFromUpload(upload);
            int[] firstLastEmailArray = createIntArrayForFirstLastEmailColumns(csvTable);
            if (intArrayHasNegativeValues(firstLastEmailArray)) return status;
            int registeredCount = 0;
            int notRegisteredCount = 0;
            foreach (DataRow row in csvTable.Rows)
            {
                EventGuest tableGuest = repository.CreateEventGuestFromRowData(row, firstLastEmailArray);
                if (repository.AddEventGuestToDbContext(tableGuest)) registeredCount++;
                else notRegisteredCount++;
            }
            status.SuccessfulUpload = true;
            status.EventGuestsUploaded = registeredCount;
            status.ColumnsNotUploaded = notRegisteredCount;
            return status;
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
            foreach (int i in array) if (i < 0) return true;
            return false;
        }
    }
}