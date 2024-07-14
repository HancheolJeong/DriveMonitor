using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Tesseract;
using System.Runtime.InteropServices;
using System.IO;
using Rect = OpenCvSharp.Rect;
using OpenCvSharp.Internal.Vectors;

/*
 * 차량 번호판 인식 프로세스
 * 1. 영상은 GrayScale 영상으로 변환
 * 2. Gaussian Blurring 노이즈 제거
 * 3. 전처리된 영상에 Edge 검출 및 Contour 그리기
 * 4. Contour 그리고 이 중 번호판 글자로 추정되는 객체 분류
 * 5. 이미지 회전 및 번호판만 남기기
 * 6. OCR 인식
 **/
namespace Service.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        //private Mat _image;
        public MainViewModel()
        {

        }
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged; // 

        /// <summary>
        /// 속성의 값이 변경될 떄 호출되는 메서드
        /// </summary>
        /// <param name="propertyName"></param>


        public String LoadAndProcessImage(string imagePath)
        {

            string test = "test";

            // 1단계 영상은 GrayScale 영상으로 변환
            Mat imgS1 = Cv2.ImRead(imagePath, ImreadModes.Grayscale); // 이미지를 1채널로 불러온다.
            Cv2.ImShow("1단계 Grayscale", imgS1);

            // 2단계 Gaussian Blurring 노이즈 제거
            Mat imgS2 = new Mat();
            Cv2.GaussianBlur(imgS1, imgS2, new Size(5, 5), 0);
            Cv2.ImShow("2단계 Gaussian Blurring", imgS2);

            // 3단계 전처리된 영상에 Edge 검출
            Mat imgS3 = new Mat();
            Cv2.Canny(imgS2, imgS3, 100, 200);
            Cv2.ImShow("3단계 Edge", imgS3);

            // 4단계 Contour 그리고 이 중 번호판글자로 추정되는 객체 분류
            Mat imgS4 = new Mat();
            Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(imgS3, out contours, out hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

            foreach (var contour in contours)
            {
                var rect = Cv2.BoundingRect(contour);
                double area = rect.Width * rect.Height;

                if ((double)rect.Height / rect.Width <= 2.5 && (double)rect.Height / rect.Width >= 0.5 && area <= 700 && area >= 100)
                {
                    Cv2.Rectangle(imgS1, rect, Scalar.Green, 1);
                }
            }
            Cv2.ImShow("Detected Plates", imgS1);
            Cv2.WaitKey(0);
            Cv2.DestroyAllWindows();

            return test;


        }









    }
}
