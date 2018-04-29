using RollCallApplication.DAL;
using RollCallApplication.Models;
using RollCallApplication.Properties;
using RollCallApplication.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RollCallApplication.Repositories
{
    public class EventGuestRepository 
    {
        private RollCallContext context;
        private EventGuestService service = new EventGuestService();

        public EventGuestRepository() => context = new RollCallContext();
        public EventGuestRepository(RollCallContext context) => this.context = context;

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
        public EventGuest GetEventGuestById(int? id)
        {
            if (id == null) return null;
            return context.EventGuests.Find(id);
        }
        public Boolean CheckInEventGuestToEvent(EventGuest guest)
        {
            EventGuest entity = context.EventGuests.Find(guest.GuestId);
            if (entity.TimeOfCheckIn != null) return false;
            entity.TimeOfCheckIn = service.GetCurrentDateTimeWithOffSet();
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