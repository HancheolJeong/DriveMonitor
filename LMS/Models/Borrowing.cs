using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Models
{
    public class Borrowing // 도서 대여 Entity
    {
        public int id {  get; set; } // id
        public int user_id { get; set; } // 사용자 ID
        public int book_id { get; set; } // 책 ID
        public DateTime borrowing_dt { get; set; } // 대여일
        public DateTime return_dt {  get; set; } // 반납일
        public char state { get; set; } // 상태 b : 빌린상태 a : 반납한 상태
        public DateTime creating_dt { get; set; } // 생성일

    }
}
