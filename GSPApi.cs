using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using BepInEx.Logging;
using Newtonsoft.Json;

namespace SkytakOpenAPI
{
    public class GSPApi
    {
        private BackgroundQueue _GSPSendQueue = new BackgroundQueue();
        private const int _OpenAPIPort = 921;
        //[Nullable(2)]
        private Socket _GSPSocket;
        private byte[] _GSPReadBuffer = new byte[1024];
        private string _GSPResponse = "";
        public static bool connected;
        private static int numberOfTimes;

        public void ConnectToGSP()
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 921);
            this._GSPSocket = new Socket(IPAddress.Parse("127.0.0.1").AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            while (!GSPApi.connected)
            {
                try
                {
                    this._GSPSocket.Connect(remoteEP);
                    GSPApi.connected = true;
                    this._GSPSocket.BeginReceive(this._GSPReadBuffer, 0, 1024, SocketFlags.None, new AsyncCallback(this.GSPReadCallback), null);
                    this.SendToGSP(JsonConvert.SerializeObject(new GSPShotData
                    {
                        DeviceID = "GC",
                        Units = "Yards",
                        APIversion = "1",
                        ShotDataOptions = new GSPShotDataOptions
                        {
                            ContainsBallData = false,
                            ContainsClubData = false,
                            IsHeartBeat = true
                        }
                    }));
                    Plugin.Log.LogInfo("Connected to GSPro OpenAPI");
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogInfo("Connecting to GSPro OpenAPI:  " + ex.Message);
                    Plugin.Log.LogInfo("Connect Retry:  " + GSPApi.numberOfTimes.ToString());
                    Thread.Sleep(5000);
                    if (GSPApi.numberOfTimes == 120)
                    {
                        this._GSPSocket = null;
                        Plugin.Log.LogInfo("Failed to connect to GSPro OpenAPI:  " + ex.Message);
                        break;
                    }
                    GSPApi.numberOfTimes++;
                }
            }
        }

        public void DisconnectFromGSP()
        {
            if (this._GSPSocket != null)
            {
                try
                {
                    Socket gspsocket = this._GSPSocket;
                    if (gspsocket != null)
                    {
                        gspsocket.Shutdown(SocketShutdown.Both);
                    }
                    return;
                }
                catch (Exception ex)
                {
                    ManualLogSource log = Plugin.Log;
                    string str = "Error can't shutdown GSPro OpenAPI socket: ";
                    Exception ex2 = ex;
                    log.LogInfo(str + ((ex2 != null) ? ex2.ToString() : null));
                    return;
                }
                finally
                {
                    Socket gspsocket2 = this._GSPSocket;
                    if (gspsocket2 != null)
                    {
                        gspsocket2.Close();
                    }
                    Socket gspsocket3 = this._GSPSocket;
                    if (gspsocket3 != null)
                    {
                        gspsocket3.Dispose();
                    }
                    GSPApi.connected = false;
                    Plugin.Log.LogInfo("Disconnected from GSPro OpenAPI");
                    this._GSPSocket = null;
                }
            }
            Plugin.Log.LogInfo("Already disconnected from GSPro OpenAPI");
        }

        public void SendToGSP(string data)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(data);
            try
            {
                Plugin.Log.LogInfo(this._GSPSocket.Send(bytes).ToString() + " Bytes Sent to GSPro OpenAPI");
            }
            catch (Exception ex)
            {
                ManualLogSource log = Plugin.Log;
                string str = "Can't send data to GSPro OpenAPI: ";
                Exception ex2 = ex;
                log.LogInfo(str + ((ex2 != null) ? ex2.ToString() : null));
            }
        }

        private void GSPReadCallback(IAsyncResult ar)
        {
            try
            {
                object asyncState = ar.AsyncState;
                int num = this._GSPSocket.EndReceive(ar);
                if (num <= 0)
                {
                    return;
                }
                this._GSPResponse = Encoding.ASCII.GetString(this._GSPReadBuffer, 0, num);
                if (this._GSPResponse.Length >= 1)
                {
                    Console.WriteLine("GSPro OpenAPI Message Received:" + Environment.NewLine + this._GSPResponse);
                }
            }
            catch (Exception)
            {
                this.DisconnectFromGSP();
                try
                {
                    Plugin.Log.LogInfo("Reconnecting to GSPro OpenAPI");
                    new Thread(new ThreadStart(Plugin.api.ConnectToGSP))
                    {
                        IsBackground = true
                    }.Start();
                }
                catch (Exception ex)
                {
                    ManualLogSource log = Plugin.Log;
                    string str = "Socket re-connect error: ";
                    Exception ex2 = ex;
                    log.LogInfo(str + ((ex2 != null) ? ex2.ToString() : null));
                }
                return;
            }
            try
            {
                this._GSPSocket.BeginReceive(this._GSPReadBuffer, 0, 1024, SocketFlags.None, new AsyncCallback(this.GSPReadCallback), null);
            }
            catch (Exception ex3)
            {
                ManualLogSource log2 = Plugin.Log;
                string str2 = "GSPro OpenAPI Response Error: ";
                Exception ex4 = ex3;
                log2.LogInfo(str2 + ((ex4 != null) ? ex4.ToString() : null));
            }
        }
    }
}
