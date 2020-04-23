using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.IO;

namespace MCUlib
{
    public class NetworkClient
    {
        TcpClient _tcpClient = new TcpClient();

        public TcpClient tcpClient
        {
            set
            {
                _tcpClient = value;
                _tcpClient.ReceiveTimeout = Constants.TimeOutServer;
                _tcpClient.SendTimeout = Constants.TimeOutServer;
            }
            get
            {
                return _tcpClient;
            }
        }

        public Exception lastExeption;

        public delegate void RecivedDataCounterChangeDelegate(long dataSize);
        public event RecivedDataCounterChangeDelegate RecivedDataCounterChange;


        public long streamedDataSizeRecived = 0;

        string host;
        int port;

        public bool Connect(string host,int port)
        {
            this.host = host;
            this.port = port;

            

           

            return Connect();
        }


        bool Connect()
        {
            _tcpClient = new TcpClient();
            _tcpClient.ReceiveTimeout = Constants.TimeOutClient;
            _tcpClient.SendTimeout = Constants.TimeOutClient;

            IPAddress ipAddress = Dns.GetHostEntry(host).AddressList[0];


            _tcpClient.Connect(host, port);

            return true;
            
        }

        public bool WriteFromStream(Stream stm, long size)
        {

            WriteInt64(size);            

            if (size > 0)
            {
                int _size;
                byte[] data = new byte[MCUlib.Constants.PackSize];

                while ((_size = stm.Read(data, 0, MCUlib.Constants.PackSize)) != 0)
                {
                    WriteBytes(data, _size);                   
                }
            }


            return true;
        }

        bool readCaclData = false;

        public long ReadToStream(Stream stm, bool readCaclDatacalculateData = false )
        {

            readCaclData = true;
            long size = ReadInt64();

            if (size > 0)
            {
                long recived_size = 0;

                do
                {
                    byte[] pack = null;
                    try
                    {
                        pack = ReadBytes(readCaclDatacalculateData);
                        recived_size += pack.Length;
                    }
                    catch (Exception ex)
                    {
                        lastExeption = ex;
                        return recived_size;
                    }

                    stm.Write(pack, 0, pack.Length);
                    
                    
                                         
                    
                }
                while (recived_size < size);
            }

            return size;
           

        }

        public bool error = false;

        public byte[] ReadBytes(bool calced = false)
        {
            NetworkStream stream = tcpClient.GetStream();
            int size = ReadInt32();
            byte[] data = ReadBytes(size);
            if (calced)
            {
                streamedDataSizeRecived += size;
                if (RecivedDataCounterChange != null)
                    RecivedDataCounterChange(streamedDataSizeRecived);
            }
            return data;
        }

        public byte[] ReadBytes(int size)
        {
            NetworkStream stream = tcpClient.GetStream();
            int readedBytes = 0;
            byte[] data = new byte[size];
            do
            {
                int readed = stream.Read(data, readedBytes, size - readedBytes);
                readedBytes += readed;
            }            
            while (readedBytes < size);            
            return data;
        }

        public void WriteBytes(byte[]data, int size=0)
        {
            NetworkStream stream = tcpClient.GetStream();
            int _size;
            if (size == 0)
                _size = data.Length;
            else
                _size = size;
            WriteInt32(_size);
            stream.Write(data, 0, _size);
        }

        public int ReadInt32()
        {
            byte[] int_data = ReadBytes(sizeof(Int32));
            return BitConverter.ToInt32(int_data, 0);            
        }

        public long ReadInt64()
        {
            byte[] int_data = ReadBytes(sizeof(Int64));
            return BitConverter.ToInt64(int_data, 0);
        }

        public void WriteInt32(int i)
        {
            NetworkStream stream = tcpClient.GetStream();
            byte[] b = BitConverter.GetBytes(i);
            stream.Write(b, 0, sizeof(Int32));
        }

        public void WriteInt64(long l)
        {
            NetworkStream stream = tcpClient.GetStream();
            byte[] b = BitConverter.GetBytes(l);
            stream.Write(b, 0, sizeof(Int64));
        }

    }


    
}
