using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.Net.Sockets;
using System.IO;
using System.Data;
using System.Diagnostics;
using System.Threading;
using MCUlib;


namespace MCUclient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        
        void saveParams()
        {
            
            Params.adress = tbAdress.Text;
            Params.port = Convert.ToInt32(tbPort.Text);
            Params.dir = tbGameDir.Text;

            Params.saveParams();
        }


        void Log(string str, params object[] args)
        {
            try
            {
                Dispatcher.Invoke((Action)delegate ()
                {
                    tbLog.Text += "[" + DateTime.Now.ToString() + "] " + string.Format(str, args) + Environment.NewLine;
                    //if (!tbLog.IsMouseOver)
                    tbLog.ScrollToLine(tbLog.LineCount - 2);
                });
            }

            catch
            {

            }
        }

        bool ModsOnly = true;



        bool Connect(NetworkClient nc, string Adress, int port, int retrys, int pause = 0)
        {
            if (nc.tcpClient.Connected)
                return true;

            bool connected = false;
            int r = 0;

            do
            {
                try
                {
                    connected = nc.Connect(Adress, port);
                }
                catch (Exception e)
                {
                    Log(e.Message);
                };
                

                if (connected == false)
                {
                    Log("Попытка соединения не удалась!");
                    if (r == retrys)
                    {
                        Log("Соединение не удалось после {0} попыток(тки)", r);
                        return false;
                    }

                    r++;
                    Thread.Sleep(1000);
                    Log("Попытка соединения {0}...", r);
                }

            }
            while (connected==false);
            return true;


        }

        public MainWindow()
        {

            InitializeComponent();
            
            webMotd.Loaded += (s, a) =>
            {
                try
                {
                    //webMotd.Refresh(true);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }

            };

            this.Width = 720;
            this.Height = 400;

            Params.loadParams();
            tbAdress.Text = Params.adress;
            tbPort.Text = Convert.ToString(Params.port);
            tbGameDir.Text = Params.dir;
            
            this.Closed += (s, a) =>
              {
                  //if (t!=null && !t.IsCompleted) t.();
                  saveParams();
              };


            tcInfo.MouseEnter += (s, a) =>
              {
                  if (!tabsExpanded)
                  {
                      ExpandTabs();
                  }
              };

            tcInfo.MouseLeave += (s, a) =>
            {
                if (!tabsExpanded)
                {
                    ColapseTabs();
                }
            };

            webMotd.MouseEnter += (s, a) =>
            {
                if (!tabsExpanded)
                {
                    ExpandTabs();
                }
            };

            tbLog.SizeChanged+=(s,a )=>
            {
                tbLog.ScrollToLine(tbLog.LineCount - 2);
            };
            

            cbClientOnly.Checked += (s, a) =>
              {
                  cbModsOnly.IsChecked = false;
                  cbClientOnly.IsEnabled = false;
                  cbModsOnly.IsEnabled = true;
                  ModsOnly = false;
              };
                        
            
            cbModsOnly.Checked += (s, a) =>
            {
                cbClientOnly.IsChecked = false;
                cbModsOnly.IsEnabled = false;
                cbClientOnly.IsEnabled = true;
                ModsOnly = true;
            };

            cbModsOnly.IsChecked = true;



            btnReconnect.IsEnabledChanged += (s, a) =>
              {
                  if ((bool)a.NewValue == true)
                  {
                      ((Image)btnReconnect.Content).Opacity = 1.0;
                  }
                  else
                  {
                      ((Image)btnReconnect.Content).Opacity = 0.5;
                  }
              };

            btnReconnect.Click += (s, a) =>
              {

                  saveParams();
                  (new Thread(delegate ()
                  {

                      CheckServer();

                  })).Start();
              };

            
           

            //tbMotd.Text = "Получение сообщения от сервера...";


            btnUpdate.IsEnabled = false;

            (new Thread(delegate ()
           {

               CheckServer();

           })).Start();




        }

        void CheckServer()
        {
            try
            {

                NetworkClient nc = new NetworkClient();
                Dispatcher.Invoke((Action)delegate () 
                {
                    btnReconnect.IsEnabled = false;
                });

                

                if ( Connect(nc, Params.adress, Params.port, 5) )
                {
                    Log("Получение сообщения дня");
                    nc.WriteInt32((int)NetworkCommands.GetMotd);
                    byte[] data = nc.ReadBytes();
                    Encoding uni = Encoding.Unicode;
                    string motd = uni.GetString(data);

                    Log("Отключение.");
                    nc.WriteInt32((int)NetworkCommands.Disconnect);

                    Dispatcher.Invoke((Action)delegate ()
                    {
                        /*
                        tbMotd.Text = motd;
                        */
                        btnReconnect.IsEnabled = true;
                        btnUpdate.IsEnabled = true;
                        
                    }
                     );
                }
                else
                {
                    Log("Ошибка!!");
                    Log("Не удалось подключится к серверу {0}:{1}",Params.adress, Params.port);
                }              
                
            }
            catch (Exception ex)
            {

                try
                {
                    Dispatcher.Invoke((Action)delegate ()
                    {
                        Log("Ошибка!!");
                        Log(ex.ToString());
                        //tbMotd.Text = "Ошибка подключения к серверу!";

                        btnReconnect.IsEnabled = true;
                    }
                    );
                }
                catch (TaskCanceledException) { }

            }
        }


        bool isOpened = false;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
            if (isOpened==false)
            {
                Storyboard sb = (Storyboard)this.Resources["PickAxeAnim1"];
                sb.Begin();
                isOpened = true;

            }
            else
            {
                Storyboard sb = (Storyboard)this.Resources["PickAxeAnim2"];
                sb.Begin();
                isOpened = false;
            }
            
            
        }        
               

        void SendCommand(Socket s, NetworkCommands comand)
        {
            byte[] b = BitConverter.GetBytes((int)comand);
            s.Send(b,sizeof(Int32),SocketFlags.None);
        }
        

        long totalRecivedSize = 0;


        void NetworkOperations()
        {
            NetworkClient nc = new NetworkClient();
            Dispatcher.Invoke(delegate () { pbLava.Value = 0; });

            /*
            nc.RecivedDataCounterChange += (l) =>
            {
                Dispatcher.Invoke((Action)delegate ()
                {
                    pbLava.Value = l;
                    tbStatistics.Text = "Загрузка" + Environment.NewLine + Convert.ToString(l) + "/" + Convert.ToString(totalRecivedSize);
                }
                    );
            };
            */

            Dispatcher.Invoke(delegate () { tbStatistics.Text = "Получение списка файлов"; });
            Thread.Sleep(100);

            if (!Connect(nc, Params.adress, Params.port, 10))
            {
                Log("Ошибка!!");
                Log("Не удалось подключится к серверу {0}:{1}", Params.adress, Params.port);
                return;
            }


            Log("Получаю список файлов.");
            nc.WriteInt32((int)NetworkCommands.GetFileList);
            byte[] data = nc.ReadBytes();

            Log("Отключение.");
            nc.WriteInt32((int)NetworkCommands.Disconnect);



            Encoding uni = Encoding.Unicode;
            string resived = uni.GetString(data);

            data = nc.ReadBytes();
            string resivedS = uni.GetString(data);

            Log("Разбираю список файлов.");
            Dispatcher.Invoke(delegate () { tbStatistics.Text = "Анализ списка файлов"; });
            DataSet ds = new DataSet();
            ds.ReadXmlSchema(new XmlTextReader(new StringReader(resivedS)));
            ds.ReadXml(new XmlTextReader(new StringReader(resived)));



            MCUlib.FileIO fio = new FileIO();
            fio.BaseDir = Params.dir;
            if (Directory.Exists(Params.dir))
                fio.CheckAllFiles();

            //исключенные
            var ExFiles_s = from table in ds.Tables[0].AsEnumerable()
                            from exfiles in Params.exFilesOnly.AsEnumerable()
                            where table.Field<string>("file").IndexOf(exfiles, StringComparison.OrdinalIgnoreCase) == 0
                            select table;
            var ExFiles_c = from table in fio.ds.Tables[0].AsEnumerable()
                            from exfiles in Params.exFilesOnly.AsEnumerable()
                            where table.Field<string>("file").IndexOf(exfiles, StringComparison.OrdinalIgnoreCase) == 0
                            select table;

            var ExFiles_str = from table in ExFiles_s
                              select table.Field<string>("file");





            //На закачку
            var q1 = from s_files in ds.Tables[0].AsEnumerable()
                     from c_files in fio.ds.Tables[0].AsEnumerable()

                     where (s_files.Field<string>("file") == c_files.Field<string>("file") &&
                           s_files.Field<string>("md5") != c_files.Field<string>("md5"))

                     select s_files;


            //DataTable table1 = q1.CopyToDataTable<DataRow>();



            IEnumerable<string> q21_str = from t in ds.Tables[0].AsEnumerable()
                                          select t.Field<string>("file");
            IEnumerable<string> q22_str = from t in fio.ds.Tables[0].AsEnumerable()
                                          select t.Field<string>("file");

            IEnumerable<string> q2_с_str = q21_str.Except(q22_str); //Тех, что нет в клиенте, для докачки
            IEnumerable<string> q2_s_str = q22_str.Except(q21_str); //Тех, что нет на сервере, для удаления
                                                                    //q2_s_str = q2_s_str.Except<string>(ExFiles_str); //исключеные файлы не трогаем

            var q2r = from t in ds.Tables[0].AsEnumerable()
                      from s in q2_с_str
                      where t.Field<string>("file") == s
                      select t;

            //DataTable table2 = q2r.CopyToDataTable<DataRow>();

            var forDownload = q1.Concat<DataRow>(q2r);
            forDownload = forDownload.Except<DataRow>(ExFiles_s);



            var forDelete = from t in fio.ds.Tables[0].AsEnumerable()
                            from s in q2_s_str
                            where t.Field<string>("file") == s
                            select t;
            forDelete = forDelete.Except<DataRow>(ExFiles_c);

            IEnumerable<string> dirs_s = from table in ds.Tables[1].AsEnumerable()
                                         select table.Field<string>("dir");
            IEnumerable<string> dirs_c = from table in fio.ds.Tables[1].AsEnumerable()
                                         select table.Field<string>("dir");

            IEnumerable<string> dirs_excepted = from table1 in dirs_c
                                                from table2 in Params.exFilesOnly.AsEnumerable()
                                                where table1.IndexOf(table2) == 0
                                                select table1;

            IEnumerable<string> dirs_fordelete = dirs_c.Except<string>(dirs_s).Except<string>(dirs_excepted);



            //урезаем запросы для модс онли
            if (ModsOnly == true)
            {
                forDownload = from table in forDownload
                              where (table.Field<string>("file").IndexOf("mods") == 0)
                              //        || (table.Field<string>("file").IndexOf("config") == 0)
                              select table;

                forDelete = from table in forDelete
                            where (table.Field<string>("file").IndexOf("mods") == 0)
                            //  || (table.Field<string>("file").IndexOf("config") == 0)
                            select table;

                dirs_fordelete = from table in dirs_fordelete
                                 where (table.IndexOf("mods") == 0)
                                 // ||        (table.IndexOf("config") == 0)
                                 select table;
            }

            var query =
                   (from files in forDownload
                   select new
                   {
                       id = files.Field<Int32>("id"),
                       name = files.Field<string>("file"),
                       size = files.Field<Int64>("size"),
                   }).ToArray();

            totalRecivedSize = 0;

            int cnt = 0;
            foreach (var row in query)
            {
                totalRecivedSize += row.size;
                cnt++;
                //Log("For download ", row.id, " ", row.name, " ", row.size);
            }

            Dispatcher.Invoke(delegate () { pbLava.Maximum = totalRecivedSize; });
            Log("Всего файлов {0} на {1} байт.", cnt, totalRecivedSize);
            Mutex mutex = new Mutex();

            nc.tcpClient.Close();
            if (cnt != 0)
            {

                long tr = 0;
                Log("Получаю файлы..");

                Dictionary<int, NetworkClient> networkDictionary = new Dictionary<int, NetworkClient>();

                ParallelOptions po = new ParallelOptions();
                po.MaxDegreeOfParallelism = 16;
                Parallel.ForEach( query, po,row => //{ }
                //foreach (var row in query)
                {
                    NetworkClient nc1;//=new NetworkClient();
                    
                    int thr_id = Thread.CurrentThread.GetHashCode();
                    if (networkDictionary.ContainsKey(thr_id))
                    {
                        nc1 = networkDictionary[thr_id];
                    }
                    else
                    {
                        nc1 = new NetworkClient();
                        networkDictionary[thr_id] = nc1;
                    }
                    while (true)
                    {
                        string path = Params.dir + System.IO.Path.DirectorySeparatorChar + System.IO.Path.GetDirectoryName(row.name) + System.IO.Path.DirectorySeparatorChar;
                       
                        string output = "";
                        long recived = 0;
                        int retry = 0;
                        output = Params.dir + System.IO.Path.DirectorySeparatorChar + row.name;
                        Directory.CreateDirectory(path);
                        FileStream f_stm = File.Create(output);

                        do
                        {
                            
                            if (!Connect(nc1, Params.adress, Params.port, 10))
                            {
                                Log("Ошибка!!");
                                Log("Не удалось подключится к серверу {0}:{1}", Params.adress, Params.port);
                                if (f_stm!=null)
                                 f_stm.Close();
                                return;
                            }
                            try
                            {
                                nc1.WriteInt32((int)NetworkCommands.GetFile2);
                                nc1.WriteInt32(row.id);
                                nc1.WriteInt64(recived);
                                recived += nc1.ReadToStream(f_stm, true);
                            }
                            catch (Exception ex)
                            {
                                Log("Ошибка!!");
                                Log("Файл: [{0}] {1} {2} байт.", row.id, row.name, row.size);
                                Log(">> ", output);
                                Log(Environment.NewLine + ex.ToString());
                                retry++;
                                Log("Не удалось принять файл ", row.name);
                                Log(Environment.NewLine + nc1.lastExeption);
                                Log("Повторное получение файла. Попытка {0}", retry);
                                nc1.tcpClient.Close();
                                Thread.Sleep(2000);
                                continue;

                            }

                        }
                        while (recived < row.size && retry <= 20);
                        f_stm.Close();

                        mutex.WaitOne();
                        tr += recived;
                        Dispatcher.Invoke((Action) delegate()
                        {
                            pbLava.Value = tr;
                            tbStatistics.Text = "Загрузка" + Environment.NewLine + Convert.ToString(tr) + "/" +
                                                Convert.ToString(totalRecivedSize);
                        });
                        mutex.ReleaseMutex();

                        if (retry == 20)
                        {
                            Log("Не удалось загрузись файл после {0} попыток", retry);
                            Log("Повторим...");
                            continue;
                        }

                        nc1.WriteInt32(-1);
                        nc1.WriteInt32((int)NetworkCommands.Disconnect);
                        nc1.tcpClient.Close();

                        break;

                    } //end (while 1)


                }//end foreach
                 );
 
                Log("Принято {0} из {1} байт.", tr, totalRecivedSize);
                Log("Завершаю прием файлов.");
                //nc.WriteInt32(-1);
                //Log("Отключение от сервера.");
                //nc.WriteInt32((int)NetworkCommands.Disconnect);
                foreach (var v in networkDictionary)
                {
                    if (v.Value.tcpClient.Connected)
                    {
                        v.Value.WriteInt32(-1);
                        v.Value.WriteInt32((int)NetworkCommands.Disconnect);
                        v.Value.tcpClient.Close();
                    }

                }
                //nc.tcpClient.Close();
            }


            //чистим директории

            Dispatcher.Invoke(delegate () { tbStatistics.Text = "Удаление лишних файлов/папок"; });
            Log("Удаление лишних файлов/папок.");


            foreach (DataRow row in forDelete)
            {
                string output = Params.dir + System.IO.Path.DirectorySeparatorChar + row.Field<string>("file");
                if (File.Exists(output))
                    File.Delete(output);
            }


            foreach (string dir in dirs_fordelete)
            {
                string path = Params.dir + System.IO.Path.DirectorySeparatorChar + dir;

                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }

            }


            Dispatcher.Invoke(delegate () { tbStatistics.Text = "Клиент обновлен!"; });

        }
        

        Task t;
        bool tabsExpanded = true;

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

            ((Button)sender).IsEnabled = false;

            saveParams();

            spProgres.Visibility = Visibility.Visible;

            t = new Task(() =>
            {

                try
                {
                    NetworkOperations();                       
                }

                catch(Exception ex)
                {

                    try
                    {
                        Dispatcher.Invoke((Action)delegate ()
                        {
                            Log("Ошибка!!");
                            Log(ex.ToString());
                            Dispatcher.Invoke(delegate () { tbStatistics.Text = "Что - то пошло не так"; });
                        });
                    }
                    catch (TaskCanceledException) { }

                }


                try
                {
                    Dispatcher.Invoke((Action)delegate ()
                    {
                        ((Button)sender).IsEnabled = true;
                        btnReconnect.IsEnabled = true;
                    });
                }
                catch (TaskCanceledException) { }
                
            });

            if (tabsExpanded)
            {
                tabsExpanded = false;
                ColapseTabs();
            }
            t.Start();
                                 

            
 
        }

        private void ColapseTabs()
        {
            Storyboard sb = (Storyboard)this.Resources["TabsColapse"];
            sb.Begin();
        }

        private void ExpandTabs()
        {
            Storyboard sb = (Storyboard)this.Resources["TabsExpand"];
            sb.Begin();
        }

        private void OptButton1_MouseEnter(object sender, MouseEventArgs e)
        {
            Storyboard sb = (Storyboard)this.Resources["OptButton1Expand"];
            sb.Begin();
        }

        private void OptButton1_MouseLeave(object sender, MouseEventArgs e)
        {
            Storyboard sb = (Storyboard)this.Resources["OptButton1Collapse"];
            sb.Begin();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            ExcludeFiles ef = new ExcludeFiles();
            ef.Owner = this;

            
            ef.files = Params.exFiles;
            ef.ShowDialog();
            Params.exFiles = ef.files;

        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start($"{Params.dir}\\jre1.8.0_251\\bin\\javaw.exe", $"-jar \"{Params.dir}\\ML.jar\"");
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка запуска! Вы сделали полное обновление?");
            }
        }
    }



    class Params
    {
        public static string dir;
        public static string adress;
        public static int port;
        public static string[] exFiles;

        static int version = 9; //версия дефолтных аругментов!

        public static string[] exFilesOnly
        {
            get
            {
                List<string> l = new List<string>();
                foreach(string s in exFiles)
                {
                    if (s.Trim()[0] != '#')
                        l.Add(s.Trim());
                }
                return l.ToArray();
            }
        }
                                            

        static string paramsPath = "";
        /*Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                                   System.IO.Path.DirectorySeparatorChar +
                                   "MCUpater";*/
        static string paramsName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)+System.IO.Path.DirectorySeparatorChar+"MCUpdate\\params.xml";



        static void Default()
        {
                dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + System.IO.Path.DirectorySeparatorChar +".minecraft";
                adress = "95.165.143.251";  //95.165.143.251
                port = 1214;
                exFiles = new string[] { "#Укажите тут файлы/папки,","#которые не будут затрагиватся при обновлении",
                                                            "#Строки, начинающиеся с # будут проигнорированы",
                                                            "#Пустые строки будут удалены",
                                                            "#","# Пример коректного исключенного файла:","#mods\\mystcraft.jar",
                                                            "options.txt", "screenshots", "saves","blueprints","logs","magic","resourcepacks",
                                                            "launcher_profiles.json","lastlogin","CustomDISkins","journeymap", "servers.dat",
                                                            "optionsshaders.txt", "usernamecache.json"
                                                        };
        }

        public static void loadParams()
        {
            
            try
            {
                
                XDocument xdoc = XDocument.Load(paramsName);
                XElement e = xdoc.Descendants("MCU").ElementAt<XElement>(0);

                int _version = Convert.ToInt32(e.Descendants("version").ElementAt<XElement>(0).Value);
                if (_version < version)
                    throw new Exception();

                adress = e.Descendants("adress").ElementAt<XElement>(0).Value;
                port = Convert.ToInt32( e.Descendants("port").ElementAt<XElement>(0).Value);
                dir = e.Descendants("MCdir").ElementAt<XElement>(0).Value;

                IEnumerable<XElement> exFileElements = e.Descendants("exFile");
                exFiles = new string[exFileElements.Count<XElement>()];

                for (int i=0; i<exFiles.Length;i++)
                {
                    exFiles[i] = exFileElements.ElementAt<XElement>(i).Value;
                }
            }

            catch
            {
                Default();
                MessageBox.Show("Установлены настройки по умолчанию!");
            }

        }

        public static void saveParams()
        {
            
            XDocument xdoc = new XDocument();
            XElement root = new XElement("MCU");
            xdoc.Add(root);

            root.Add(new XElement("adress", adress));
            root.Add(new XElement("port", port));
            root.Add(new XElement("MCdir", dir));
            root.Add(new XElement("version", version));

            for (int i = 0; i < exFiles.Length; i++)
            {
                root.Add(new XElement("exFile", exFiles[i]));
            }

            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(paramsName));
            xdoc.Save(paramsName);
            
        }
    }
}
