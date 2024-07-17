using Service.Vehicle; // Vehicle 네임스페이스 선언
using Service.ViewModels;
using System.Collections.ObjectModel; // 데이터 컬렉션 관련 클래스
using System.IO; // 입출력 관련 네임스페이스
using System.Net; // 네트워크 관련 네임스페이스
using System.Net.Sockets; // TCP/IP 소켓 관련 네임스페이스
using System.Text; // 문자열 처리 네임스페이스
using System.Windows; //WPF 응용 프로그램 네임스페이스
using System.Windows.Media.Imaging;

namespace Service.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainViewModel _mainViewModel { get; set; }
        private TcpListener _listener; // TCP 소켓 선언 
        private Task<string> plateNumber;
        private const int Port = 11000; // 포트번호

        public MainWindow()
        {
            InitializeComponent(); // XAML 정의 요소 초기화
            _mainViewModel = new MainViewModel();
            DataContext = _mainViewModel; // 데이터 바인딩을 위한 데이터 컨텍스트 설정
            _mainViewModel.LoadVehicles();
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
            try
            {
                string imagePath = await ReceiveFile(stream); // 파일 수신
                string plateNumber = await _mainViewModel.ProcessImage(imagePath);
                await _mainViewModel.IncomingInsertData(plateNumber, DateTime.Now, imagePath);
                byte[] response = Encoding.UTF8.GetBytes("0"); // 응답 객체 생성
                await stream.WriteAsync(response, 0, response.Length); // 응답 전송
            }
            catch (Exception ex) 
            {
                MessageBox.Show($"오류가 발생했습니다. {ex.Message}");
                byte[] response = Encoding.UTF8.GetBytes("-1"); // 응답 객체 생성
                await stream.WriteAsync(response, 0, response.Length); // 응답 전송
            }
        }

        private async Task HandleOutgoingVehicle(NetworkStream stream)
        {
            // 데이터베이스로부터 들어온 시간과 나간시간의 차를 구하고 이를 주차요금으로 환산한다.
            int cost = 3000;
            DateTime current_time = DateTime.Now;

            string imagePath = await ReceiveFile(stream); // 파일 수신
            string plateNumber = await _mainViewModel.ProcessImage(imagePath); // OCR 수행

            DateTime incoming_time; // 차량이 들어왔던 시간
            TimeSpan timeDifference; // 시간차를 저장하는 객체

            try
            {
                incoming_time = await _mainViewModel.GetIncomingDate(plateNumber);
                timeDifference = current_time - incoming_time;

                // 30분 이하인 경우, 기본 요금은 3000원
                if (timeDifference.TotalMinutes <= 30)
                {
                    cost = 3000;
                }
                else
                {
                    // 30분을 초과하는 경우, 기본 요금에 10분마다 1000원 추가
                    double extraMinutes = timeDifference.TotalMinutes - 30;
                    int extraCharges = (int)Math.Ceiling(extraMinutes / 10) * 1000; // 여기를 수정
                    cost += extraCharges;
                }

                byte[] costBytes = BitConverter.GetBytes(cost); // 요금값을 바이트로 변환해 배열에 넣는다
                await stream.WriteAsync(costBytes, 0, costBytes.Length); // 요금정보를 전송
            }
            catch(Exception ex)
            {
                byte[] costBytes = BitConverter.GetBytes(-1); // 요금값을 바이트로 변환해 배열에 넣는다
                await stream.WriteAsync(costBytes, 0, costBytes.Length); // 요금정보를 전송
                Console.WriteLine($"오류가 발생했습니다. {ex.Message}");
            }


            byte[] buffer = new byte[sizeof(int)]; // 4바이트 버퍼 생성
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length); // 스트림에서 데이터를 읽는다
            int clientCost = BitConverter.ToInt32(buffer, 0); // 읽은 데이터를 문자열로 변환

            if (clientCost == cost)
            {
                byte[] vehicleNumberBytes = Encoding.UTF8.GetBytes(plateNumber); // 차량 번호
                await stream.WriteAsync(vehicleNumberBytes, 0, vehicleNumberBytes.Length); // 차량 번호 전송
                await _mainViewModel.OutgoingUpdateData(plateNumber, current_time, imagePath, cost); // 데이터베이스 나간시간, 이미지 경로, 요금 업데이트
            }
            else
            {
                byte[] errorBytes = Encoding.UTF8.GetBytes("-1");
                await stream.WriteAsync(errorBytes, 0, errorBytes.Length); // 오류 발생 시 -1 전송
            }


        }

        /**
         * 비동기 + 들어오는 차량 나가는 차량 몰리면 이름이 겹칠 가능성 있을듯...? 아마도...?
         */
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
