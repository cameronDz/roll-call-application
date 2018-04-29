using RollCallApplication.DAL;
using RollCallApplication.Models;
using RollCallApplication.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace RollCallApplication.Services
{
    public class EventGuestService
    {
        private RollCallContext context = new RollCallContext();

        public byte[] FullEventGuestListInCsv()
        {
            IEnumerable<EventGuest> rows = context.EventGuests;
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
    }
}