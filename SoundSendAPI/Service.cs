using NAudio.Wave;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SoundSendAPI
{
    static class WaveInfo
    {
        public static WaveInEvent waveIn { get; set; }
        public static Int64 cnt { get; set; } //seq rtp header
    }

    internal class Service : IService
    {
        UdpClient cli = new UdpClient(); //udp server로 데이터를 던진다

        byte vhfIdBy = 0, remoteBy = 0;//장비정보

        byte[] byteArr = new byte[160]; //실 packet data
        int byteCnt = 0; //160 packet마다 자르기 위해서
        int timeMsSinceMidnight = (int)DateTime.Now.TimeOfDay.TotalMilliseconds;

        String flag = "";

        // /data/{value}의 형시긍로 접속되면 호출되어 처리한다.
        public Response GetResponse(string value, string vhfId, string remote)
        {
            flag = value;
            vhfIdBy = Convert.ToByte(vhfId);
            remoteBy = Convert.ToByte(remote);

            Console.WriteLine("vhfId : " + vhfId + ", remote : " + remote);

            if (value == "start")
            {
                StartRecording();
            }
            else
            {
                RecordEnd();
            }
            // Response 클래스 타입으로 리턴하면 자동으로 Json형식으로 변환한다.
            return new Response() { Result = "200" };
        }


        public void StartRecording()
        {
            //Console.WriteLine("들어옴");
            //sourceStream = new WaveIn 
            //{
            //    DeviceNumber = this.InputDeviceIndex,
            //    WaveFormat =
            //        new WaveFormat(8000, 16, 1),
            //    BufferMilliseconds = 100
            //};

            //sourceStream.DataAvailable += this.SourceStreamDataAvailable;

            //sourceStream.StartRecording();

            WaveInfo.waveIn = new WaveInEvent();
            WaveInfo.waveIn.DeviceNumber = 0;
            WaveInfo.waveIn.WaveFormat = new WaveFormat(8000, 16, 1);
            //WaveInfo.waveIn.BufferMilliseconds = 100;
            WaveInfo.waveIn.DataAvailable += this.SourceStreamDataAvailable;
            WaveInfo.waveIn.StartRecording();
        }

        public void RecordEnd()
        {
            if (WaveInfo.waveIn != null)
            {
                WaveInfo.waveIn.StopRecording();
                WaveInfo.waveIn.Dispose();
                WaveInfo.waveIn = null;
            }
        }

        public const int BIAS = 0x84; //132, or 1000 0100
        public const int MAX = 32635; //32767 (max 15-bit integer) minus BIAS


        private static byte encode(int pcm) //16-bit
        {
            //Get the sign bit. Shift it for later 
            //use without further modification
            int sign = (pcm & 0x8000) >> 8;
            //If the number is negative, make it 
            //positive (now it's a magnitude)
            if (sign != 0)
                pcm = -pcm;
            //The magnitude must be less than 32635 to avoid overflow
            if (pcm > MAX) pcm = MAX;
            //Add 132 to guarantee a 1 in 
            //the eight bits after the sign bit
            pcm += BIAS;

            /* Finding the "exponent"
            * Bits:
            * 1 2 3 4 5 6 7 8 9 A B C D E F G
            * S 7 6 5 4 3 2 1 0 . . . . . . .
            * We want to find where the first 1 after the sign bit is.
            * We take the corresponding value from
            * the second row as the exponent value.
            * (i.e. if first 1 at position 7 -> exponent = 2) */
            int exponent = 7;
            //Move to the right and decrement exponent until we hit the 1
            for (int expMask = 0x4000; (pcm & expMask) == 0;
                 exponent--, expMask >>= 1) { }

            /* The last part - the "mantissa"
            * We need to take the four bits after the 1 we just found.
            * To get it, we shift 0x0f :
            * 1 2 3 4 5 6 7 8 9 A B C D E F G
            * S 0 0 0 0 0 1 . . . . . . . . . (meaning exponent is 2)
            * . . . . . . . . . . . . 1 1 1 1
            * We shift it 5 times for an exponent of two, meaning
            * we will shift our four bits (exponent + 3) bits.
            * For convenience, we will actually just shift
            * the number, then and with 0x0f. */
            int mantissa = (pcm >> (exponent + 3)) & 0x0f;

            //The mu-law byte bit arrangement 
            //is SEEEMMMM (Sign, Exponent, and Mantissa.)
            byte mulaw = (byte)(sign | exponent << 4 | mantissa);

            //Last is to flip the bits
            return (byte)~mulaw;
        }

        public void SourceStreamDataAvailable(object sender, WaveInEventArgs e)
        {
            Console.WriteLine(flag + "  buffer Size : " +e.Buffer.Length);
            

            int value;
            
            int bytesPerSample = 2;


            //string time = Convert.ToString(timeMsSinceMidnight, 2).PadLeft(32, '0');

            //byte by1 = (byte)Convert.ToInt32(time.Substring(0, 8), 2);
            //byte by2 = (byte)Convert.ToInt32(time.Substring(8, 8), 2);
            //byte by3 = (byte)Convert.ToInt32(time.Substring(16, 8), 2);
            //byte by4 = (byte)Convert.ToInt32(time.Substring(24, 8), 2);

            //for(int index = 0; index < e.BytesRecorded; index++)
            //{
            //    Console.WriteLine(e.Buffer[index]);
            //}



            for (int index = 0; index < e.BytesRecorded; index += bytesPerSample)
            {

                value = BitConverter.ToInt16(e.Buffer, index);
                byte encodeData = encode(value);

                //byteArr[byteCnt] = encodeData;
                byteCnt++;

                if (index > 0)
                {
                    Console.Write(encodeData + "/ ");
                }
                //else
                //{
                //    Console.Write(e.Buffer[0] + ", " + e.Buffer[1] + "/");
                //}


                /* 실시간 처리 함수 */
                if (byteCnt == 160)
                {
                    timeMsSinceMidnight++;
                    WaveInfo.cnt++;

                    byte[] bytes = BitConverter.GetBytes(timeMsSinceMidnight);
                    byte[] cntbyte = BitConverter.GetBytes(WaveInfo.cnt);

                    //Console.WriteLine("160///////////////////////////////////////////////////////////////");
                    byte[] header = new byte[12];
                    header[0] = 128;
                    header[1] = 0;
                    header[2] = cntbyte[1];
                    header[3] = cntbyte[0];
                    //header[4] = 0;
                    //header[5] = 0;
                    //header[6] = 0;
                    //header[7] = 0;
                    header[4] = bytes[3];
                    header[5] = bytes[2];
                    header[6] = bytes[1];
                    header[7] = bytes[0];
                    header[8] = vhfIdBy;
                    header[9] = remoteBy;
                    header[10] = 0;
                    header[11] = 0;


                    byte[] result = header.Concat(byteArr).ToArray();



                    //if(vhfIdBy == 51)
                    //{
                    //    //부산항
                    //    ip = "192.168.1.109";
                    //    port = 10353;
                    //}
                    //else
                    //{
                    //    //울산항
                    //    ip = "192.168.60.108";
                    //    port = 10253;
                    //}
                    ////cli.Send(result, result.Length, "155.155.4.93", 10353);
                    //string ip = "155.155.4.210";
                    //int port = 10353;
                    //cli.Send(result, result.Length, ip, port);

                    byteCnt = 0;
                    byteArr = new byte[160];

                    Console.WriteLine("////////////////////////////////////////////////" + WaveInfo.cnt);
                }


                //Console.WriteLine(value + ", byte :" + encodeData + ", index : " + index);
            }
        }

    }
}
