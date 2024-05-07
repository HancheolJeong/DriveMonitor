using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LMS.ViewModels
{
    public class MainViewModel: INotifyPropertyChanged
    {
        public MainViewModel()
        {
            
        }

        public event PropertyChangedEventHandler? PropertyChanged; // 

        /// <summary>
        /// 속성의 값이 변경될 떄 호출되는 메서드
        /// </summary>
        /// <param name="propertyName"></param>
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
