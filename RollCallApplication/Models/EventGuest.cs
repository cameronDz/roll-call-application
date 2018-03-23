using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace RollCallApplication.Models
{
    public class EventGuest
    {
        [Key]
        [Display(Name = "Guest Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int GuestId { get; set; }
        [Display(Name = "First Name")]
        public String FirstName { get; set; }
        [Display(Name = "Last Name")]
        public String LastName { get; set; }
        [Display(Name = "Email Address")]
        public String Email { get; set; }
        [Display(Name = "Check In Time")]
        public DateTime TimeOfCheckIn { get; set; }
    }
}