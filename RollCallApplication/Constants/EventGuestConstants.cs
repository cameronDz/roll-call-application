using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RollCallApplication.Constants
{
    public class EventGuestConstants
    {
        public static readonly String ADMIN_PASSCODE_DEFAULT = "Enter passcode to view guests check in times and make edit/deletions to guests.";
        public static readonly String ADMIN_PASSCODE_WARNING = "Must enter passcode to view any list of guests and make edit/deletions to guests that have checked in through application";
        public static readonly String CHECK_IN_DEFAULT_MESSAGE = "Preregister Guest Check In List.";
        public static readonly String CHECK_IN_FAIL = "Unable to check into event. Please try again..";
        public static readonly String REGISTRATION_DEFAULT = "Register or Check In an unregistered Guest for Event.";
        public static readonly String REGISTRATION_EXISTING_ERROR = "Failed Registration. Email already registered on database under:";
        public static readonly String LOAD_REGISTRANTS_DEFAULT = "Load Registration List through .csv file.";
        public static readonly String LOAD_REGISTRANTS_FILE_TYPE_ERROR = "Failed Upload. Unable to load registrants due to upload not being .csv file.";
        public static readonly String LOAD_REGISTRANTS_MISSING_COLUMN_ERROR = "Failed Upload. Unable to load registrants due to column name issue is .csv file.";
        public static readonly String LOAD_REGISTRANTS_TABLE_ERROR = "Failed Upload. Error on in table.";
        public static readonly String LOAD_REGISTRANTS_UPLOAD_ERROR = "Failed Upload. Upload never arrived.";
    }
}