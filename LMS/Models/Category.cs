using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Models
{
    public class Category // 항목 Entity
    {
        public int id { get; set; } // id
        public string name { get; set; } // 이름
        public DateTime creating_dt { get; set; } // 생성일
    }
}
