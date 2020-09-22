using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Model
{
    public class User
    {
        [AutoIncrement, PrimaryKey]
        public int ID { get; set; }
        public string UserName { get; set; }
        public string UserNumber { get; set; }
        public int useLevel { get; set; }
    }
}
