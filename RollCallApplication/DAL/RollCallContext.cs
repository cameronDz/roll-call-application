using RollCallApplication.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace RollCallApplication.DAL
{
    public class RollCallContext : DbContext
    {
        public RollCallContext() : base("rollCallConnectionString")
        {
        }
        public DbSet<EventGuest> EventGuests { get; set; }
    }
}