using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpClientApp
{
    class Program
    {
        private const string ServerIp = "127.0.0.1";
        private const int ServerPort = 11000;

        static async Task Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("명령행 인자가 올바르지 않습니다.");
                return;
            }

            string command = args[0];
            string imagePath = args[1];

            using TcpClient client = new TcpClient();
            await client.ConnectAsync(ServerIp, ServerPort);

            NetworkStream stream = client.GetStream();
            byte[] commandBytes = Encoding.UTF8.GetBytes(command);
            await stream.WriteAsync(commandBytes, 0, commandBytes.Length);

            byte[] fileBytes = File.ReadAllBytes(imagePath);
            byte[] fileLengthBytes = BitConverter.GetBytes(fileBytes.Length);
            await stream.WriteAsync(fileLengthBytes, 0, fileLengthBytes.Length);
            await stream.WriteAsync(fileBytes, 0, fileBytes.Length);

            if (command == "i")
            {
                await HandleIncomingVehicle(stream);
            }
            else if (command == "o")
            {
                await HandleOutgoingVehicle(stream);
            }
        }

        private static async Task HandleIncomingVehicle(NetworkStream stream)
        {
            byte[] buffer = new byte[1];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string vehicleNumber = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            if (vehicleNumber == "-1")
            {
                Console.WriteLine("주차관리시스템에서 오류를 전송했습니다. 다시 시도해주세요.");
            }
            else
            {
                Console.WriteLine($"[{vehicleNumber}] 환영합니다.");
            }
        }

        private static async Task HandleOutgoingVehicle(NetworkStream stream)
        {
            byte[] buffer = new byte[sizeof(int)];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            int fee = BitConverter.ToInt32(buffer, 0);

            if (fee == -1)
            {
                Console.WriteLine("주차관리시스템에서 오류를 전송했습니다. 다시 시도해주세요.");
                return;
            }

            Console.WriteLine($"요금은 {fee}원 입니다.");

            // 요금 재전송 정상적으로 요금을 수납했다고 알리는 작업
            string tmpFeeVerify = Console.ReadLine(); // 고객이 수납한 요금
            int feeVerify = int.TryParse(tmpFeeVerify, out int res ) ? res : -1; // int로 형변환
            byte[] feeBytes = BitConverter.GetBytes(feeVerify); // 서버소켓으로 전송
            await stream.WriteAsync(feeBytes, 0, feeBytes.Length); // 응답읽기

            // 차량 번호 수신
            buffer = new byte[4]; // 4바이트크기
            bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string vehicleNumber = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            if (vehicleNumber == "-1")
            {
                Console.WriteLine("주차관리시스템에서 오류를 전송했습니다. 다시 시도해주세요.");
            }
            else
            {
                Console.WriteLine($"[{vehicleNumber}] 감사합니다 안녕히가세요.");
            }

            //// 클라이언트가 종료되지 않고 다음 작업을 대기
            //Console.WriteLine("Press any key to exit...");
            //Console.ReadKey();
        }
    }
}