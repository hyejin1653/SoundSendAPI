using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace SoundSendAPI
{
    

    [ServiceContract]
    public interface IService
    {
        [OperationContract]
        // 메소드 지정과 리턴 포멧 지정(리턴 포멧은 RestAPI를 만들 것이기 때문에 Json으로 한다.)
        // UriTemplate에 보간법으로 중괄호로 값을 지정할 수 있다.
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, UriTemplate = "/vhf/{flag}/{vhfId}/{remote}")]
        // 이 어트리뷰트를 설정하지 않으면 기본 함수명을 키로 리턴이 됩니다. 여기서는 키를 value로 지정했다.
        [return: MessageParameter(Name = "flag")]
        // 리턴을 String으로 해도 상관없는데.. 여기서는 클래스 형식한다.
        // 클래스 형식으로 하면 자동으로 Newtonsoft.Json 라이브러리를 통해 자동으로 Json 형식으로 변환한다.

        Response GetResponse(string flag, string vhfId, string remote);
    }

    // 그냥 String이로
    public class Response
    {
        // 프로퍼티
        public string Result { get; set; }
    }

}
