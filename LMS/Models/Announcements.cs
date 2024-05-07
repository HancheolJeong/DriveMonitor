using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Models
{
    public class Announcements // 공지 Entity
    {
        public int id { get; set; } // id
        public string title {  get; set; } // 제목
        public string context { get; set; } // 내용
        public int manager_id { get; set; } // 관리자 ID
        public DateTime creating_dt { get; set; }  // 생성일

    }
}
