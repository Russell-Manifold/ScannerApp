using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Model
{
    public class DocHeader
    {
        [AutoIncrement, PrimaryKey]
        public int ID { get; set; }
        public string  DocNum { get; set; }
        public string AcctCode { get; set; }
        public string AccName { get; set; }
        public int DocContolUser { get; set; }
        public DateTime DocumentDate { get; set; }
        public int PackerUser { get; set; }
        public int PickerUser { get; set; }
        public int AuthUser { get; set; }
        public string DeliveryAddress1 { get; set; }
        public string OrderNumber { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

    }
}
