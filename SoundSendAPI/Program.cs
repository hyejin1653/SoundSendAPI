using System;
using NAudio.Wave.SampleProviders;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using System.Net.Sockets;
using System.ServiceModel.Web;
using System.ServiceModel;

namespace SoundSendAPI
{
    class Program
    {
        static void Main(string[] args)
        {
            // 서버 인스턴스 생성
            var server = new WebServiceHost(typeof(Service));
            // EndPoint 설정
            server.AddServiceEndpoint(typeof(IService), new WebHttpBinding(), "");
            // 서버 기동
            server.Open();
            // 콘솔 출력
            Console.WriteLine("Server start.");
            // 엔터 키를 누르면 종료 메시지.
            Console.WriteLine("If you want exit this application, please push enter key.");
            // Main 함수가 끝나면 프로그램이 종료되니, 서버 스레드가 움직일 수 있도록 대기 한다.
            Console.ReadLine();
        }
    }

}

