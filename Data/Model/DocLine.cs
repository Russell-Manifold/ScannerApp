using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace Data.Model
{
    public class DocLine
    {
        [AutoIncrement, PrimaryKey]
        public int ID { get; set; }
        public string DocNum { get; set; }
        public string SupplierCode { get; set; }
        public string SupplierName { get; set; }
        public string ItemBarcode { get; set; }
        public string ItemCode { get; set; }
        public string ItemDesc { get; set; }
        public string WarehouseID { get; set; }
        public bool isRejected { get; set; }
        public int ItemQty { get; set; }
        public int ScanAccQty { get; set; }
        public int ScanRejQty { get; set; }
        public int Balacnce { get; set; }
        public string Complete { get; set; }
        public int PalletNum { get; set; }
        public string Bin { get; set; }
        public string GLCode { get; set; }
        public bool GRN { get; set; }
    }
}
