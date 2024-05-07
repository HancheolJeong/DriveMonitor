using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Models
{
    public class Manager // 관리자 entity
    {
        public int id { get; set; } // id
        public string pw { get; set; } // 패스워드
        public string name { get; set; } // 이름
        public string phone_nbr { get; set; } // 휴대폰 번호
        public DateTime creating_dt { get; set; } // 생성일
    }
}
