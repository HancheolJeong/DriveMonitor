using OpenCvSharp;
using System.ComponentModel;
using System.Reflection;
using Tesseract;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;
using Rect = OpenCvSharp.Rect;
using System.IO;
using System.Windows.Media;
using System.Windows;
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

        public event PropertyChangedEventHandler? PropertyChanged;

        public String LoadAndProcessImage(string imagePath)
        {

            string test = "test";


            // 1단계 영상은 GrayScale 영상으로 변환
            Mat imgS1 = Cv2.ImRead(imagePath, ImreadModes.Grayscale); // 이미지를 1채널로 불러온다.
            //Cv2.Resize(imgS1, imgS1, new Size(300, 100)); // 각 데이터세트마다 이미지 크기가 달라서 해당 작업이 필요하다
            //Cv2.ImShow("1단계 Grayscale", imgS1);
            int width = imgS1.Width;
            int height = imgS1.Height;

            // 2단계 Gaussian Blurring 노이즈 제거
            Mat imgS2 = new Mat();
            Cv2.GaussianBlur(imgS1, imgS2, new Size(5, 5), 0);
            //Cv2.ImShow("2단계 Gaussian Blurring", imgS2);

            // 3단계 전처리된 영상에 Edge 검출
            Mat imgS3 = new Mat();
            Cv2.Canny(imgS2, imgS3, 100, 200);
            Cv2.ImShow("3단계 Edge", imgS3);


            // 4단계 Contour 검출 - 외곽선 좌표를 모두 추출
            Mat imgS4 = new Mat();
            Point[][] contours;
            HierarchyIndex[] hierarchy; // 외곽선 계층 정보
            Cv2.FindContours(imgS3, out contours, out hierarchy, RetrievalModes.List, ContourApproximationModes.ApproxSimple);
            List<Point[]> contours_poly = new List<Point[]>(contours.Length);
            imgS4 = Mat.Zeros(imgS3.Size(), MatType.CV_8UC3);
            List<Rect> boundRect = new List<Rect>();
            List<Rect> boundRect2 = new List<Rect>();
            List<Rect> boundRect3 = new List<Rect>();

            for (int i = 0; i < contours.Length; i++) // Contours 정보 추가 하는 로직
            {
                contours_poly.Add(Cv2.ApproxPolyDP(contours[i], 1, true));
                //boundRect.Add(Cv2.BoundingRect(contours_poly[i])); // Contour 정보 추가
                Rect newRect = Cv2.BoundingRect(contours_poly[i]); // 새로운 Contour 정보 생성

                bool isOverlap = false;
                for (int j = 0; j < boundRect.Count; j++)
                {
                    if (newRect.IntersectsWith(boundRect[j])) // 중복 검사
                    {
                        isOverlap = true;
                        if ((newRect.Width * newRect.Height) > (boundRect[j].Width * boundRect[j].Height)) // 새로운 사각형이 더 큰 경우
                        {
                            boundRect[j] = newRect; // 기존 사각형을 새로운 사각형으로 교체
                            break;
                        }
                    }
                }
                if (!isOverlap)
                {
                    boundRect.Add(newRect); // 중복이 없는 경우 바로 추가
                }
            }



            int refinery_count = 0;

            for (int i = 0; i < boundRect.Count; i++)
            {
                double ratio = (double)boundRect[i].Height / boundRect[i].Width;

                    //if ((boundRect[i].Width * boundRect[i].Height <= 2000) && (boundRect[i].Width * boundRect[i].Height >= 200))
                if ((ratio <= 3.0) && (ratio >= 0.6) && (boundRect[i].Width * boundRect[i].Height <= 2000) && (boundRect[i].Width * boundRect[i].Height >= 200))
                {
                    Cv2.DrawContours(imgS4, contours, i, new Scalar(0, 255, 255), 1, LineTypes.Link8, hierarchy, 0); // DrawContour는 외곽선 그리는 함수
                    Cv2.Rectangle(imgS4, boundRect[i].TopLeft, boundRect[i].BottomRight, new Scalar(255, 0, 0), 1, LineTypes.Link8); //외곽선을 채우는 사각형을 그리는 함수
                    /*
                     저장되는 정보 X, Y, Width, Height, Top, Bottom, Left, Right, Location(point), Size(size), TopLeft(x,y), BottomRight(x,y)a
                     */
                    boundRect2.Add(boundRect[i]); // 사각형 좌표정보를 저장합니다.
                    refinery_count += 1;
                }
            }
            Cv2.ImShow("4단계 Contour", imgS4);


            // 5단계 이미지 Crop
            // Median 계산
            List<int> areas = boundRect.Select(rect => rect.Width * rect.Height).ToList();
            areas.Sort();
            int medianArea = areas[areas.Count / 2];
            int lowerBound = (int)(medianArea * 0.70);
            int upperBound = (int)(medianArea * 1.30);

            // Median 주변값 필터링
            List<Rect> filteredRects = boundRect.Where(rect => (rect.Width * rect.Height >= lowerBound && rect.Width * rect.Height <= upperBound)).ToList();

            // 최대 TopLeft와 BottomRight 찾기
            Point topLeft = new Point(filteredRects.Min(rect => rect.Left), filteredRects.Min(rect => rect.Top));
            Point bottomRight = new Point(filteredRects.Max(rect => rect.Right), filteredRects.Max(rect => rect.Bottom));

            Size rectSize = new Size(bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
            // 영역 Crop 및 출력
            Rect numberPlateRect = new Rect(topLeft, rectSize);
            Mat numberPlateImg = new Mat(imgS1, numberPlateRect);
            Cv2.ImShow("Detected Number Plate", numberPlateImg);

            Cv2.GaussianBlur(numberPlateImg, numberPlateImg, new Size(5, 5), 0);
            Cv2.AdaptiveThreshold(numberPlateImg, numberPlateImg, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, 19, 9);

            Cv2.ImWrite("temp.jpg", numberPlateImg);
            Cv2.ImShow("result", numberPlateImg);


            //Cv2.ImWrite("temp.jpg", imgS7);
            //Cv2.ImShow("result", imgS7);

            Cv2.WaitKey(0);
            Cv2.DestroyAllWindows();


            test = GetLicensePlateNumber("temp.jpg");
            return test;


        }

        public string GetLicensePlateNumber(string imagePath)
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            string licensePlateNumber = "";
            path = Path.Combine(path, "tessdata");
            path = path.Replace("file:\\", "");
            using (var engine = new TesseractEngine(path, "kor", EngineMode.TesseractOnly))
            using (var img = Pix.LoadFromFile(imagePath))
            {
                using (var page = engine.Process(img, PageSegMode.SingleLine))
                {
                    string tempLicensePlateNumber = page.GetText();
                    licensePlateNumber = ExtractLicensePlateNumber(tempLicensePlateNumber);

                }
            }
            return licensePlateNumber;
        }

        private string ExtractLicensePlateNumber(string text)
        {
            text = text.Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "");
            var regex = new System.Text.RegularExpressions.Regex("\\d{2,3}[가-힣]\\d{4}");
            var match = regex.Match(text);
            return match.Success ? match.Value : "OCR 인식 오류 : " + text;
        }
    }
}
