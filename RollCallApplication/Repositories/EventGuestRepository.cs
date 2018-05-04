using RollCallApplication.DAL;
using RollCallApplication.Models;
using RollCallApplication.Properties;
using RollCallApplication.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace RollCallApplication.Repositories
{
    public class EventGuestRepository 
    {
        private RollCallContext context;
        private EventGuestService service;
        public EventGuestRepository()
        {
            context = new RollCallContext();
            service = new EventGuestService(this);
        }
        public EventGuestRepository(RollCallContext context)
        {
            this.context = context;
            service = new EventGuestService(this);
        }
        public List<EventGuest> GetOrderedEventGuestListWithSearchParam(String sortOrder, String searchParam)
        {
            List<EventGuest> fullGuestList = context.EventGuests.ToList();
            if (String.IsNullOrEmpty(searchParam)) return orderListOfGuests(sortOrder, fullGuestList);
            else searchParam = searchParam.ToLower();
            List<EventGuest> limitedGuestList = new List<EventGuest>();
            foreach (EventGuest guest in fullGuestList)
            {
                if (guest.FirstName.ToLower().StartsWith(searchParam)) limitedGuestList.Add(guest);
                else if (guest.LastName.ToLower().StartsWith(searchParam)) limitedGuestList.Add(guest);
                else if (guest.Email.ToLower().StartsWith(searchParam)) limitedGuestList.Add(guest);
            }
            return orderListOfGuests(sortOrder, limitedGuestList);
        }
        public Boolean AddEventGuestToDbContext(EventGuest guest)
        {
            if (String.IsNullOrEmpty(guest.Email)) return false;
            if (context.EventGuests.Count(g => g.Email.Equals(guest.Email)) > 0) return false;
            if (guest.Preregistered == false) guest.TimeOfCheckIn = service.GetCurrentDateTimeWithOffSet();
            context.EventGuests.Add(guest);
            context.SaveChanges();
            return true;
        }
        public Boolean UncheckInEventGuest(int id)
        {
            EventGuest eventGuest = context.EventGuests.Find(id);
            if (eventGuest == null) return false;
            eventGuest.TimeOfCheckIn = null;
            context.Entry(eventGuest).State = EntityState.Modified;
            context.SaveChanges();
            return true;
        }
        public EventGuest CreateEventGuestFromRowData(DataRow row, int[] firstLastEmailArray)
        {
            EventGuest guest = new EventGuest();
            guest.FirstName = row.ItemArray.ElementAt(firstLastEmailArray[0]).ToString();
            guest.LastName = row.ItemArray.ElementAt(firstLastEmailArray[1]).ToString();
            guest.Email = row.ItemArray.ElementAt(firstLastEmailArray[2]).ToString();
            guest.Preregistered = true;
            return guest;
        }
        public EventGuest GetEventGuestById(int? id)
        {
            if (id == null) return null;
            return context.EventGuests.Find(id);
        }
        public Boolean CheckInEventGuestToEvent(int? id)
        {
            EventGuest entity = context.EventGuests.Find(id);
            if (entity == null) return false;
            if (entity.TimeOfCheckIn != null) return false;
            entity.TimeOfCheckIn = service.GetCurrentDateTimeWithOffSet();
            context.SaveChanges();
            return true;
        }
        public Boolean DeleteEventGuestById(int id)
        {
            EventGuest eventGuest = context.EventGuests.Find(id);
            context.EventGuests.Remove(eventGuest);
            context.SaveChanges();
            return true;
        }
        public int GetNumberOfCheckedInGuests()
        {
            return context.EventGuests.Count(g => g.TimeOfCheckIn != null);
        }
        public Boolean UpdateEventGuest(EventGuest guest)
        {
            context.Entry(guest).State = EntityState.Modified;
            context.SaveChanges();
            return true;
        }
        private List<EventGuest> orderListOfGuests(String sortOrder, List<EventGuest> list)
        {
            if (String.IsNullOrEmpty(sortOrder)) return list.OrderBy(g => g.LastName).ToList();
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
                case "time_of_check_in_desc":
                    return list.OrderByDescending(g => g.TimeOfCheckIn).ToList();
                case "time_of_check_in_ascd":
                    return list.OrderBy(g => g.TimeOfCheckIn).ToList();
                default:
                    return list.OrderBy(g => g.LastName).ToList();
            }
        }
    }
}