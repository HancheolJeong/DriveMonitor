using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Models
{
    public class Book // 책 entity
    {
        public int id {  get; set; } // ID
        public string title { get; set; } // 제목
        public int author_id { get; set; } // 작가 ID
        public int publisher_id { get; set; } // 출판사 ID
        public int category_id { get; set; } // 카테고리 ID
        public int publishing_year { get; set; } // 출판년도
        public DateTime creating_dt { get; set; }// 생성일
        

    }
}
