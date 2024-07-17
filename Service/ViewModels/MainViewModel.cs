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
using Service.Models;
using System.Data.SqlClient;
using System.Data;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows.Input;
using Microsoft.Win32;
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
        private List<Car> _incomingVehicles = new List<Car>(); // 들어온 차량 정보를 저장하는 리스트
        private List<Car> _outgoingVehicles = new List<Car>(); // 나간 차량 정보를 저장하는 리스트
        private BitmapImage _incomingVehicleImage; // 들어온 차량 번호 이미지
        private BitmapImage _outgoingVehicleImage; // 나간 차량 번호 이미지
        private string _incomingVehicleToday; // 오늘 들어온 차량 수
        private string _outgoingVehicleToday; // 오늘 나간 차량 수
        private string _costToday; // 오늘 요금

        public List<Car> IncomingVehicles
        {
            get { return _incomingVehicles; }
            set
            {
                _incomingVehicles = value;
                OnPropertyChanged(nameof(IncomingVehicles));
            }
        }

        public List<Car> OutgoingVehicles
        {
            get { return _outgoingVehicles; }
            set
            {
                _outgoingVehicles = value;
                OnPropertyChanged(nameof(OutgoingVehicles));
            }
        }

        public BitmapImage IncomingVehicleImage
        {
            get { return _incomingVehicleImage; }
            set
            {
                _incomingVehicleImage = value;
                OnPropertyChanged(nameof(IncomingVehicleImage));
            }
        }

        public BitmapImage OutgoingVehicleImage
        {
            get { return _outgoingVehicleImage; }
            set
            {
                _outgoingVehicleImage = value;
                OnPropertyChanged(nameof(OutgoingVehicleImage));
            }
        }

        public string IncomingVehicleToday
        {
            get { return _incomingVehicleToday; }
            set
            {
                _incomingVehicleToday = value;
                OnPropertyChanged(nameof(IncomingVehicleToday));
            }
        }

        public string OutgoingVehicleToday
        {
            get { return _outgoingVehicleToday; }
            set
            {
                _outgoingVehicleToday = value;
                OnPropertyChanged(nameof(OutgoingVehicleToday));
            }
        }
        public string CostToday
        {
            get { return _costToday; }
            set
            {
                _costToday = value;
                OnPropertyChanged(nameof(CostToday));
            }
        }


        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public async Task<string> ProcessImage(string imagePath)
        {

            string text = "../../../default";


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
            //Cv2.ImShow("3단계 Edge", imgS3);


            // 4단계 Contour 검출 - 외곽선 좌표를 모두 추출
            Mat imgS4 = new Mat();
            Point[][] contours;
            HierarchyIndex[] hierarchy; // 외곽선 계층 정보
            Cv2.FindContours(imgS3, out contours, out hierarchy, RetrievalModes.List, ContourApproximationModes.ApproxSimple);
            List<Point[]> contours_poly = new List<Point[]>(contours.Length);
            imgS4 = Mat.Zeros(imgS3.Size(), MatType.CV_8UC3);
            List<Rect> boundRect = new List<Rect>();
            List<Rect> boundRect2 = new List<Rect>();

            for (int i = 0; i < contours.Length; i++) // Contours 정보 추가 하는 로직
            {
                contours_poly.Add(Cv2.ApproxPolyDP(contours[i], 1, true));
                //boundRect.Add(Cv2.BoundingRect(contours_poly[i])); // Contour 정보 추가
                Rect newRect = Cv2.BoundingRect(contours_poly[i]); // 새로운 Contour 정보 생성

                bool isOverlap = false; // 중복검사 변수
                for (int j = 0; j < boundRect.Count; j++) // Retangle 좌표가 겹치는 부분이 있을때 상대적으로 넓이가 작은 Rectangle을 제거하는 로직
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

                if ((ratio <= 3.0) && (ratio >= 0.6) && (boundRect[i].Width * boundRect[i].Height <= 2000) && (boundRect[i].Width * boundRect[i].Height >= 100)) // 높이 너비 비율이 3이하 0.6이하 이면서 넓이가 2000 이하 200 이상 이어야함
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
            //Cv2.ImShow("4단계 Contour", imgS4);


            // 5단계 이미지 Crop
            // Median 계산
            List<int> areas = boundRect2.Select(rect => rect.Width * rect.Height).ToList();
            areas.Sort();
            int medianArea = areas[areas.Count / 2];
            int lowerBound = (int)(medianArea * 0.70);
            int upperBound = (int)(medianArea * 1.30);

            // Median 주변값 필터링
            List<Rect> filteredRects = boundRect2.Where(rect => (rect.Width * rect.Height >= lowerBound && rect.Width * rect.Height <= upperBound)).ToList();

            // 최대 TopLeft와 BottomRight 찾기
            Point topLeft = new Point(filteredRects.Min(rect => rect.Left), filteredRects.Min(rect => rect.Top));
            Point bottomRight = new Point(filteredRects.Max(rect => rect.Right), filteredRects.Max(rect => rect.Bottom));

            Size rectSize = new Size(bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
            // 영역 Crop 및 출력
            Rect numberPlateRect = new Rect(topLeft, rectSize);
            Mat numberPlateImg = new Mat(imgS1, numberPlateRect);
            //Cv2.ImShow("Detected Number Plate", numberPlateImg);

            Cv2.GaussianBlur(numberPlateImg, numberPlateImg, new Size(5, 5), 0);
            Cv2.AdaptiveThreshold(numberPlateImg, numberPlateImg, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, 19, 9);

            Cv2.ImWrite("temp.jpg", numberPlateImg);
            //Cv2.ImShow("result", numberPlateImg);


            text = GetLicensePlateNumber("temp.jpg");

            return text;


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
            text = text.Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", ""); // 공백, 개행, 탭을 빈문자열로 치환
            var regex = new System.Text.RegularExpressions.Regex("\\d{2,3}[가-힣]\\d{4}"); // 2~3숫자 + 한글 + 4숫자 조합
            var match = regex.Match(text);
            return match.Success ? match.Value : "OCR 인식 오류 : " + text;
        }

        public async Task LoadVehicles()
        {
            await LoadIncomingVehicles();
            await LoadOutgoingVehicles();
            await LoadBoardToday();
        }

        public async Task<DateTime> GetIncomingDate(string plate_number)
        {
            List<Car> cars = new List<Car>();
            DataSet ds = new DataSet();
            DateTime time = DateTime.Now;
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(Properties.Settings.Default.ConnectionStr))
                {
                    await sqlConnection.OpenAsync();
                    SqlDataAdapter sqlDataAdapter = new SqlDataAdapter("SELECT input_date FROM car WHERE plate_number = '" + plate_number + "' AND output_date IS NULL ORDER BY input_date DESC", sqlConnection);
                    sqlDataAdapter.Fill(ds);
                }

                if (ds.Tables.Count > 0)
                {
                    DataTable dt = ds.Tables[0];
                    foreach (DataRow row in dt.Rows)
                    {
                        Car car = new Car
                        {
                            input_date = Convert.ToDateTime(row["input_date"].ToString()) // 들어온 시간 저장
                        };
                        cars.Add(car); // 리스트에 Car 추가
                    }
                }
                if (cars.Count > 0) // 데이터가 1개라도 존재하는지 확인
                {
                    time = cars[0].input_date;
                }

            }
            catch (Exception ex)
            {
                // 예외 처리 로직
                Console.WriteLine(ex.Message);
            }

            return time;
        }
        private async Task LoadIncomingVehicles()
        {
            List<Car> tempIncomingVehicles = new List<Car>();
            DataSet ds = new DataSet();
            string path = "../../../Resources/default.jpg";
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(Properties.Settings.Default.ConnectionStr))
                {
                    await sqlConnection.OpenAsync();
                    SqlDataAdapter sqlDataAdapter = new SqlDataAdapter("SELECT TOP 50 plate_number, input_date, input_path FROM car ORDER BY input_date DESC", sqlConnection);
                    sqlDataAdapter.Fill(ds);
                }

                if (ds.Tables.Count > 0)
                {
                    DataTable dt = ds.Tables[0];
                    foreach (DataRow row in dt.Rows)
                    {
                        Car car = new Car
                        {
                            plate_number = row["plate_number"].ToString(), // 차량번호 저장
                            input_date = Convert.ToDateTime(row["input_date"]), // 들어온 시간 저장
                            input_path = row["input_path"].ToString() // 들어온 차량 번호 이미지 경로 저장
                        };
                        tempIncomingVehicles.Add(car); // 리스트에 Car 추가
                    }
                }
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IncomingVehicles = tempIncomingVehicles;
                });
                if (tempIncomingVehicles.Count > 0) // 데이터가 1개라도 존재하는지 확인
                {
                    path = tempIncomingVehicles[0].input_path;
                }
                await IncomingDisplayImage(path);

            }
            catch (Exception ex)
            {
                Console.WriteLine("데이터베이스 오류가 발생했습니다.: " + ex.Message);
            }
        }

        private async Task LoadOutgoingVehicles()
        {
            List<Car> tempOutgoingVehicles = new List<Car>();
            DataSet ds = new DataSet();
            string path = "../../../Resources/default.jpg";
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(Properties.Settings.Default.ConnectionStr))
                {
                    await sqlConnection.OpenAsync();
                    SqlDataAdapter sqlDataAdapter = new SqlDataAdapter("SELECT TOP 50 plate_number, cost, output_date, output_path FROM car WHERE output_date IS NOT NULL ORDER BY output_date DESC", sqlConnection);
                    sqlDataAdapter.Fill(ds);
                }

                if (ds.Tables.Count > 0)
                {
                    DataTable dt = ds.Tables[0];
                    foreach (DataRow row in dt.Rows)
                    {
                        Car car = new Car
                        {
                            plate_number = row["plate_number"].ToString(), // 차량번호 저장
                            cost = Convert.ToInt32(row["cost"]), // 요금 저장
                            output_date = Convert.ToDateTime(row["output_date"]), // 나간 시간 저장
                            output_path = row["output_path"].ToString() // 나간 차량 번호 이미지 경로 저장
                        };
                        tempOutgoingVehicles.Add(car); // 리스트에 Car 추가
                    }
                }
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    OutgoingVehicles = tempOutgoingVehicles;
                });
                if (tempOutgoingVehicles.Count > 0) // 데이터가 1개라도 존재하는지 확인
                {
                    path = tempOutgoingVehicles[0].output_path;
                }
                await OutgoingDisplayImage(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("데이터베이스 오류가 발생했습니다.: " + ex.Message);
            }
        }

        private async Task LoadBoardToday()
        {
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(Properties.Settings.Default.ConnectionStr))
                {
                    await sqlConnection.OpenAsync();
                    string query = @"
                SELECT 
                    (SELECT COUNT(*) FROM car WHERE CONVERT(varchar, input_date, 23) = CONVERT(varchar, GETDATE(), 23)) AS i,
                    (SELECT COUNT(*) FROM car WHERE CONVERT(varchar, output_date, 23) = CONVERT(varchar, GETDATE(), 23)) AS o,
                    (SELECT ISNULL(SUM(cost), 0) FROM car WHERE CONVERT(varchar, output_date, 23) = CONVERT(varchar, GETDATE(), 23)) AS c";

                    SqlCommand sqlCommand = new SqlCommand(query, sqlConnection);
                    SqlDataReader reader = await sqlCommand.ExecuteReaderAsync();

                    if (reader.Read())
                    {
                        int i = reader.GetInt32(reader.GetOrdinal("i")); // 오늘 들어온 차량수
                        int o = reader.GetInt32(reader.GetOrdinal("o")); // 오늘 나간 차량수
                        int c = reader.GetInt32(reader.GetOrdinal("c")); // 오늘 확보한 요금

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            IncomingVehicleToday = $"들어온 차량 : {i}";
                            OutgoingVehicleToday = $"나간 차량 : {o}";
                            CostToday = $"요금 : {c}";
                        });
                        }

                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("데이터베이스 오류가 발생했습니다.: " + ex.Message);
            }
        }

        private async Task IncomingDisplayImage(string path) // 들어온 차량 이미지 바인딩 함수
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                string fullPath = Path.GetFullPath(path); // 상대경로를 절대경로로 바꾼다
                BitmapImage image = new BitmapImage(new Uri(fullPath)); // 비트맵 이미지로 변경
                IncomingVehicleImage = image; // 이미지 바인딩
            });
        }

        private async Task OutgoingDisplayImage(string path) // 나간 차량 이미지 바인딩 함수
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                string fullPath = Path.GetFullPath(path); // 상대경로를 절대경로로 바꾼다
                BitmapImage image = new BitmapImage(new Uri(fullPath)); // 비트맵 이미지로 변경
                OutgoingVehicleImage = image; // 이미지 바인딩
            });
        }

        public async Task IncomingInsertData(string plate_number, DateTime input_date, string input_path)
        {

            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(Properties.Settings.Default.ConnectionStr))
                {
                    await sqlConnection.OpenAsync();
                    using (SqlCommand sqlCommand = new SqlCommand("INSERT INTO car(plate_number, input_date, input_path) VALUES(@plate_number, @input_date, @input_path)", sqlConnection))
                    {
                        sqlCommand.Parameters.AddWithValue("@plate_number", plate_number);
                        sqlCommand.Parameters.AddWithValue("@input_date", input_date);
                        sqlCommand.Parameters.AddWithValue("@input_path", input_path);

                        await sqlCommand.ExecuteNonQueryAsync();
                    }
                }
                await LoadIncomingVehicles();
                await LoadBoardToday();

            }
            catch (Exception ex)
            {
                Console.WriteLine("데이터베이스 오류가 발생했습니다.: " + ex.Message);
            }

        }

        public async Task OutgoingUpdateData(string plate_number, DateTime output_date, string output_path, int cost)
        {

            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(Properties.Settings.Default.ConnectionStr))
                {
                    await sqlConnection.OpenAsync();
                    // DATEDIFF로 시간차를 구하고 30분이내면 3000원 30분초과시 3000 + 10분마다 1500원씩 추가된다.
                    using (SqlCommand sqlCommand = new SqlCommand("UPDATE car SET output_date = @output_date, output_path = @output_path, cost = @cost WHERE plate_number=@plate_number AND output_date IS NULL", sqlConnection))
                    {
                        sqlCommand.Parameters.AddWithValue("@output_date", output_date);
                        sqlCommand.Parameters.AddWithValue("@output_path", output_path);
                        sqlCommand.Parameters.AddWithValue("@cost", cost);
                        sqlCommand.Parameters.AddWithValue("@plate_number", plate_number);

                        await sqlCommand.ExecuteNonQueryAsync();
                    }
                }
                await LoadOutgoingVehicles();
                await LoadBoardToday();
            }
            catch (Exception ex)
            {
                Console.WriteLine("데이터베이스 오류가 발생했습니다.: " + ex.Message);
            }

        }

    }
}
