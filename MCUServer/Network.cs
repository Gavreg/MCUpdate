using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Timers;
using System.IO;

using MCUlib;

namespace MCUServer
{
     


    class IncomingRequestArgs
    {
        public NetworkClient client;
        public NetworkCommands command;        
        public string ip;
    };


    class TCPserver
    {


        TcpListener sListener;
        Params parameters;

        public delegate bool IncomingRequestDelegate(object sender, IncomingRequestArgs args);
        public event IncomingRequestDelegate IncomingRequest;

        public TCPserver(Params p)
        {
            parameters = p;

        }


        void ThreadStart(object o)
        {

            TcpListener l = (TcpListener)o;
            l.Start();
            
            while (true)
            {              
               
                try
                {
                    TcpClient s = l.AcceptTcpClient();
                    NetworkClient c = new NetworkClient();
                    c.tcpClient = s;                


                    Thread t = new Thread(this.newConnection);
                    t.Start(c);
                }
                catch (Exception ex)
                {
                    Program.Log("ERROR!!!");                    
                    Program.Log(ex.ToString());
                    break;
                }
            }
        }

        public void StartServer()
        {
            IPHostEntry ipHost = Dns.GetHostEntry("127.0.0.1");
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, parameters.port);
            sListener = new TcpListener(ipEndPoint);
            Thread t = new Thread(this.ThreadStart);
            t.Start(sListener); 
            
            Program.Log("Server under {0} sratred", parameters.port);

                       
        }


        

        void newConnection(object obj)
        {
            NetworkClient soc = (NetworkClient)obj;
                        
            string ip =  ((IPEndPoint)soc.tcpClient.Client.RemoteEndPoint).Address.ToString();

            Program.Log("[{0}] New connection.", ip);
            try
            {
                bool NoErrors = true;
                while (soc.tcpClient.Connected && NoErrors)
                {

                    Program.Log("[{0}] Waiting for command.", ip);
                    int comandInt = soc.ReadInt32();                        

                    IncomingRequestArgs new_args = new IncomingRequestArgs();
                    new_args.client = soc;
                    new_args.ip = ip;
                    new_args.command = (NetworkCommands)comandInt;                    


                    NoErrors = IncomingRequest(this, new_args);                               

                }                


            }
            catch (Exception ex)
            {
                Program.Log("[{0}] ERROR!!!", ip);
                Program.Log("[{0}] " + ex.ToString(), ip);

                if (soc.tcpClient.Connected)
                    soc.tcpClient.Close();                           
            }

         Program.Log("[{0}] Thread stopped", ip);
         soc.tcpClient.Close();
        }

        
    }
}

