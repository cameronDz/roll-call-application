using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RollCallApplication.Models
{
    public class CsvUploadStatus
    {
        public Boolean SuccessfulUpload { get; set; }
        public int EventGuestsUploaded { get; set; }
        public int ColumnsNotUploaded { get; set; }
        public String ErrorMessage { get; set; }
    }
}