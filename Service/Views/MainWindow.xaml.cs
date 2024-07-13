using Service.Vehicle; // Vehicle 네임스페이스 선언
using System.Collections.ObjectModel; // 데이터 컬렉션 관련 클래스
using System.IO; // 입출력 관련 네임스페이스
using System.Net; // 네트워크 관련 네임스페이스
using System.Net.Sockets; // TCP/IP 소켓 관련 네임스페이스
using System.Text; // 문자열 처리 네임스페이스
using System.Windows; //WPF 응용 프로그램 네임스페이스
using System.Windows.Media.Imaging; 

namespace Service
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TcpListener _listener; // TCP 소켓 선언 
        private const int Port = 11000; // 포트번호
        public ObservableCollection<VehicleInfo> IncomingVehicles { get; set; } = new ObservableCollection<VehicleInfo>(); // 들어온 차량 정보 객체 선언
        public ObservableCollection<VehicleInfo> OutgoingVehicles { get; set; } = new ObservableCollection<VehicleInfo>(); // 나간 차량 정보 객체 선언

        public MainWindow()
        {
            InitializeComponent(); // XAML 정의 요소 초기화
            DataContext = this; // 데이터 바인딩을 위한 데이터 컨텍스트 설정
            StartTcpServer(); // TCP 서버 시작
        }

        private async void StartTcpServer()
        {
            _listener = new TcpListener(IPAddress.Any, Port); // TCP 리스너 객체 초기화
            _listener.Start(); // 리스너 시작

            while (true)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(); //클라이언트 연결 객체 생성
                _ = Task.Run(() => HandleClient(client)); // 클라이언트 소켓과의 작업을 비동기적으로 수행
            }
        }

        private async void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream(); // 스트림 객체 생성
            byte[] buffer = new byte[1]; // 1바이트 버퍼 생성
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length); // 데이터를 읽는다
            string command = Encoding.UTF8.GetString(buffer, 0, bytesRead); // 데이터를 문자열로 변환

            if (command == "i") // 들어오는 차량에 대한 요청 처리
            {
                await HandleIncomingVehicle(stream);
            }
            else if (command == "o") // 나가는 차량에 대한 요청 처리
            {
                await HandleOutgoingVehicle(stream);
            }

            client.Close();
        }

        private async Task HandleIncomingVehicle(NetworkStream stream)
        {
            string imageResource = await ReceiveFile(stream); // 파일 수신

            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var image = new BitmapImage(new Uri(imageResource, UriKind.Absolute)); // 비트맵 이미지 생성
                    IncomingVehicleImage.Source = image; // WPF 이미지 컨트롤 소스 이미지 지정

                    IncomingVehicles.Add(new VehicleInfo // 리스트뷰 요소 추가
                    {
                        VehicleNumber = "0000", // 차량 번호
                        Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") // 들어온 시간
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"WPF 컨트롤 작업 중 오류가 발생했습니다.: {ex.Message}");
                }
            });

            byte[] response = Encoding.UTF8.GetBytes("0"); // 응답 객체 생성
            await stream.WriteAsync(response, 0, response.Length); // 응답 전송
        }

        private async Task HandleOutgoingVehicle(NetworkStream stream)
        {
            string imageResource = await ReceiveFile(stream); // 파일 수신

            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var image = new BitmapImage(new Uri(imageResource, UriKind.Absolute)); // 비트맵 이미지 생성
                    OutgoingVehicleImage.Source = image; // WPF 이미지 컨트롤 소스 이미지 지정

                    OutgoingVehicles.Add(new VehicleInfo // 리스트뷰 요소 추가
                    {
                        VehicleNumber = "0001", // 차량 번호
                        Fee = 1000, // 요금
                        Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") // 나간 시간
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"WPF 컨트롤 작업 중 오류가 발생했습니다.: {ex.Message}");
                }
            });

            int fee = 1000;
            byte[] feeBytes = BitConverter.GetBytes(fee); // 요금값을 바이트로 변환해 배열에 넣는다
            await stream.WriteAsync(feeBytes, 0, feeBytes.Length); // 요금정보를 전송

            byte[] buffer = new byte[sizeof(int)]; // 4바이트 버퍼 생성
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length); // 스트림에서 데이터를 읽는다
            string clientFee = Encoding.UTF8.GetString(buffer, 0, bytesRead); // 읽은 데이터를 문자열로 변환

            // 데이터베이스에 데이터를 저장하는 작업이 필요하다.
            if (clientFee == fee.ToString())
            {
                byte[] vehicleNumberBytes = Encoding.UTF8.GetBytes("0001"); // 차량 번호
                await stream.WriteAsync(vehicleNumberBytes, 0, vehicleNumberBytes.Length); // 차량 번호 전송
            }
            else
            {
                byte[] errorBytes = BitConverter.GetBytes(-1);
                await stream.WriteAsync(errorBytes, 0, errorBytes.Length); // 오류 발생 시 -1 전송
            }

            //byte[] response = Encoding.UTF8.GetBytes(clientResponse == "0" ? "0" : "1"); // 임시
            //await stream.WriteAsync(response, 0, response.Length); //임시
        }

        private async Task<string> ReceiveFile(NetworkStream stream)
        {
            byte[] lengthBuffer = new byte[4]; // 파일 길이를 저장할 버퍼
            await stream.ReadAsync(lengthBuffer, 0, 4); // 스트림에서 파일 길이를 읽어옴
            int fileLength = BitConverter.ToInt32(lengthBuffer, 0); // 파일 길이를 int로 변환

            byte[] fileBuffer = new byte[fileLength]; // 파일 내용을 저장할 버퍼 생성
            int bytesRead = 0; // 읽은 바이트 수
            while (bytesRead < fileLength) // 모든 파일내용을 읽는다.
            {
                bytesRead += await stream.ReadAsync(fileBuffer, bytesRead, fileLength - bytesRead); // 파일 내용을 읽는다.
            }
            string projectDirectory = AppDomain.CurrentDomain.BaseDirectory; // 프로젝트의 경로를 가져온다.
            string resourcesDirectory = Path.Combine(projectDirectory, "../../../Resources"); // 프로젝트 경로의 하위 디렉터리 Resources를 붙인다.

            if (!Directory.Exists(resourcesDirectory)) // Resources 디렉터리 있는지 확인
            {
                Directory.CreateDirectory(resourcesDirectory); // 새로운 디렉터리를 생성한다.
            }

            string fileName = $"image_{DateTime.Now:yyyyMMdd_HHmmss_fff}.jpg"; // 파일명이 겹치지 않게 하기 위해 밀리초까지 포함시킨다.
            string filePath = Path.Combine(resourcesDirectory, fileName); // 디렉터리 + 파일명 지정

            await File.WriteAllBytesAsync(filePath, fileBuffer); // 파일을 저장한다.

            return filePath;
        }
    }

}
