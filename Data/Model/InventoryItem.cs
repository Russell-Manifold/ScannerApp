using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Model
{
    public class InventoryItem
    {
        [AutoIncrement, PrimaryKey]
        public int ID { get; set; }
        public int CountID { get; set; }
        public string ItemDesc { get; set; }
        public double FirstScanQty { get; set; }
        public double SecondScanQty { get; set; }
        public int SecondScanAuth { get; set; }
        public int CountUser { get; set; }
        public bool isFirst { get; set; }
        public bool Complete { get; set; }
        public string BarCode { get; set; }
        public string ItemCode { get; set; }       
        public double FinalQTY { get; set; }       
        public string Bin { get; set; }       
        public string Status { get; set; }       
    }
}
