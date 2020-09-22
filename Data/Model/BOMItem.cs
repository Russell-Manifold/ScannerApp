using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace Data.Model
{
    public class BOMItem
    {
        [AutoIncrement,PrimaryKey]
        public int ID { get; set; }
        public string PackBarcode { get; set; }
        public string ItemCode { get; set; }
        public string ItemDesc { get; set; }
        public int Qty { get; set; }
    }
}
