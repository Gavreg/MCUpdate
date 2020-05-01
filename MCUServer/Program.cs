using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;
using System.Data;
using MCUlib;
using System.Net;
using System.Net.Sockets;


using System.IO;

namespace MCUServer
{

    class Program
    {

        static FileIO f;
        static Params serverParams;
        static string filelist = String.Empty;
        static string sceme = String.Empty;

        static string motd;

        static FileStream logFile = null;

        public static void Log(string msg, params object[] args)
        {
            string log = string.Format("[" + DateTime.Now + "] " + msg, args);
            //Console.WriteLine( log);

            Encoding uni = Encoding.UTF8;
            byte[] data = uni.GetBytes(log+Environment.NewLine);
            logFile.Write(data, 0, data.Length);
            logFile.Flush();
                    
        }

        static void Main(string[] args)
        {

            if (!File.Exists("log.txt"))
                File.Create("log.txt").Close();
            logFile = new FileStream("log.txt", FileMode.Append, FileAccess.Write);
            

            StreamReader sr = new StreamReader("motd.txt");
            
            motd = sr.ReadToEnd();
            sr.Close();


            f = new MCUlib.FileIO();
            Program.Log("CreatingFileList...");
            try
            {
                f.CheckAllFiles();
            }           

            catch(Exception ex)
            {
                Program.Log("Error!!");
                Program.Log(ex.Message);
                Console.ReadLine();
                Environment.Exit(0);
            }


            XDocument xdoc = new XDocument();
            XElement root = new XElement("root");
            xdoc.Add(root);

            foreach (DataRow row in f.ds.Tables[0].Rows)
            {             
                Program.Log("ID:{0}  FILE:{1} size:{2} md5:{3}", row["id"], row["file"], row["size"], row["md5"]);                
            }

            filelist = f.ds.GetXml();
            sceme = f.ds.GetXmlSchema();

            Program.Log("Completed!");


            serverParams = ParamsLoad.loadParams();
            TCPserver tcp = new TCPserver(serverParams);
            tcp.IncomingRequest += Tcp_IncomingRequest;
            tcp.StartServer();

            Console.ReadLine();

        }

        private static bool Tcp_IncomingRequest(object sender, IncomingRequestArgs args)
        {

            string ip = args.ip;
           
            if (args.command == NetworkCommands.GetFileList)
            {
                Program.Log("[{0}] GetFileList",ip);
                Encoding uni = Encoding.Unicode;

                try
                {
                    byte[] data = uni.GetBytes(filelist);
                    args.client.WriteBytes(data);

                    data = uni.GetBytes(sceme);
                    args.client.WriteBytes(data);
                }

                catch (Exception ex)
                {
                    Program.Log("[{0}] ERROR!!!", ip);
                    Program.Log("[{0}] " + ex.ToString(), ip);
                    return false;
                }


                return true;
            }
            if (args.command == NetworkCommands.GetMotd)
            {
                Program.Log("[{0}] GetMOTD", ip);
                Encoding uni = Encoding.Unicode;
                byte[] data = uni.GetBytes(motd);
                args.client.WriteBytes(data);

                return true;
            }

            if (args.command == NetworkCommands.GetFilesReconnect)
            {
                FileStream stream = null;

                try
                {
                    Program.Log("[{0}] GetFilesReconnect", ip);
                    Program.Log("[{0}] Waiting for id.", ip);
                    int id = args.client.ReadInt32();
                    Program.Log("[{0}] Waiting for pos.", ip);
                    long pos = args.client.ReadInt64();
                    string file = f.findById(id);
                    Program.Log("[{0}] Send file {1} from {2}", ip, file, pos);
                    System.IO.FileInfo fi = new System.IO.FileInfo(file); //поменять.... сделать для класс для этого
                    stream = new FileStream(file, FileMode.Open, FileAccess.Read);
                    stream.Seek(pos, SeekOrigin.Begin);
                    args.client.WriteFromStream(stream, fi.Length-pos);
                    stream.Close();
                    return true;
                }

                catch (Exception ex)
                {
                    Program.Log("[{0}] ERROR!!!", ip);
                    Program.Log("[{0}] " + ex.ToString(), ip);
                    if (stream != null)
                        stream.Close();
                    return false;
                }


            }

            if (args.command == NetworkCommands.GetFiles)
            {
                Program.Log("[{0}] GetFiles", ip);
                while (true)
                {
                    FileStream stream = null;
                    try
                    {
                        Program.Log("[{0}] Waiting for id.", ip);
                        int id = args.client.ReadInt32();

                        Program.Log("[{0}] Requesting file {1}", ip, Convert.ToString(id));
                        if (id < 0)
                            break;
                        
                        string file = f.findById(id);
                        Program.Log("[{0}] Send file {1}", ip, file);
                        System.IO.FileInfo fi = new System.IO.FileInfo(file); //поменять.... сделать для класс для этого
                        stream = new FileStream(file, FileMode.Open, FileAccess.Read);
                        args.client.WriteFromStream(stream, fi.Length);
                        stream.Close();
                    }
                    catch (Exception ex)
                    {
                        Program.Log("[{0}] ERROR!!!", ip);
                        Program.Log("[{0}] " + ex.ToString(), ip);
                        if (stream != null)
                            stream.Close();
                        return false;
                    }
        

                }
                return true;
            }
            

            if (args.command == NetworkCommands.GetFile2)
            {
                FileStream stream = null;

                try
                {
                    
                    int id = args.client.ReadInt32();
                    long pos = args.client.ReadInt64();
                    string file = f.findById(id);
                    Program.Log("[{0}] Send file {1} from {2}", ip, file, pos);
                    System.IO.FileInfo fi = new System.IO.FileInfo(file); //поменять.... сделать для класс для этого
                    stream = new FileStream(file, FileMode.Open, FileAccess.Read);
                    stream.Seek(pos, SeekOrigin.Begin);
                    args.client.WriteFromStream(stream, fi.Length - pos);
                    stream.Close();
                    return true;
                }

                catch (Exception ex)
                {
                    Program.Log("[{0}] ERROR!!!", ip);
                    Program.Log("[{0}] " + ex.ToString(), ip);
                    if (stream != null)
                        stream.Close();
                    return false;
                }
            }
            if (args.command == NetworkCommands.Disconnect)
            {
                Program.Log("[{0}] Disconect.", ip);
                args.client.tcpClient.Close();
                
            }

            return true;
        }

        static void readConfig()
        {

        }


    }
}
