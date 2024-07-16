using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models
{
    public class Car
    {
        public int id { get; set; } //ID

        public string plate_number { get; set; } // 차량번호

        public DateTime input_date { get; set; } // 들어온 시간
    
        public DateTime? output_date { get; set; } // 들어온 시간

        public string input_path { get; set; } // 들어온 차량 이미지 경로

        public string? output_path { get; set; } // 나간 차량 이미지 경로

        public int? cost { get; set; } // 요금
    
    }
}
