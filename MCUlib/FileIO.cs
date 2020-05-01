using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;

using System.Security.Cryptography;
using System.Threading;

namespace MCUlib
{
    public class FileIO
    {
        public List<FileInfo> files = new List<FileInfo>();

        public DataSet ds;

        public FileIO()
        {
            BaseDir = "Base";

            ds =  new DataSet();

            DataTable filesTable = new DataTable();
            ds.Tables.Add(filesTable);

            DataTable dirsTable = new DataTable();
            ds.Tables.Add(dirsTable);

            filesTable.Columns.Add(new DataColumn("id", typeof(Int32)));            
            filesTable.Columns.Add(new DataColumn("file", typeof(String)));
            filesTable.Columns.Add(new DataColumn("md5", typeof(String)));
            filesTable.Columns.Add(new DataColumn("size", typeof(Int64)));

            dirsTable.Columns.Add(new DataColumn("id", typeof(Int32)));
            dirsTable.Columns.Add(new DataColumn("dir", typeof(string)));
            //dirsTable.Columns.Add(new DataColumn("fileCount", typeof(Int32)));
            dirsTable.Columns.Add(new DataColumn("totalFileCount", typeof(Int32)));
            //dirsTable.Columns.Add(new DataColumn("dirCount", typeof(Int32)));

        }

        public string findById (int id)
        {

            var query =
                from files in ds.Tables[0].AsEnumerable()
                where files.Field<int>("id") == id
                select new
                {
                    id = files.Field<int>("id"),
                    name = BaseDir + System.IO.Path.DirectorySeparatorChar + files.Field<string>("file")
                };
            foreach (var row in query)
            {
                return row.name;
            }

            throw new Exception();
        }
       
        public string BaseDir
        {
            get; set;
        }


        public void CheckAllFiles(bool files = true, bool dirs = true)
        {
            CheckAllFiles(BaseDir, files,  dirs);
        }

        byte[] getFileMD5(string file)
        {
            MD5 md5 = MD5.Create();
            var stream = File.OpenRead(file);
            return md5.ComputeHash(stream);
            
        }

        int fileID = 0;
        int dirID = 0;

        void CheckAllFiles(string startdir, bool files = true, bool dirs = true)
        {
            if (files)
            {
                String[] f = System.IO.Directory.GetFiles(startdir, "*", SearchOption.AllDirectories);
                List<DataRow> tmp_table = new List<DataRow>();
                Parallel.ForEach(f, (s) =>
                    //foreach (string s in f)
                {

                    DataRow row = ds.Tables[0].NewRow();
                    int id = Interlocked.Increment(ref fileID);
                    row["id"] = id; //  Convert.ToString( fileID++);
                    row["file"] = s.Remove(0, BaseDir.Length + 1);
                    System.IO.FileInfo _fi = new System.IO.FileInfo(s);
                    row["size"] = _fi.Length;
                    byte[] data = getFileMD5(s);
                    StringBuilder sBuilder = new StringBuilder();
                    for (int i = 0; i < data.Length; i++)
                    {
                        sBuilder.Append(data[i].ToString("x2"));
                    }

                    row["md5"] = sBuilder.ToString();
                    tmp_table.Add(row);

                });
                foreach (var r in tmp_table)
                    ds.Tables[0].Rows.Add(r);
            }

            if (dirs)
            {
                String[] d = System.IO.Directory.GetDirectories(startdir, "*", SearchOption.AllDirectories);

                foreach (string s in d)
                {
                    DataRow row = ds.Tables[1].NewRow();
                    row["id"] = dirID++;
                    row["dir"] = s.Remove(0, BaseDir.Length + 1);
                    System.IO.DirectoryInfo _di = new System.IO.DirectoryInfo(s);
                    //row["fileCount"] = _di.GetFiles("*", SearchOption.TopDirectoryOnly).Length;
                    row["totalFileCount"] = _di.GetFiles("*", SearchOption.AllDirectories).Length;
                   // row["dirCount"] = _di.GetDirectories("*", SearchOption.TopDirectoryOnly).Length;
                    ds.Tables[1].Rows.Add(row);
                }
            }


        }

    };

    public class FileInfo
    {
        public string name;
        public string path;
        public string md5;
        public long size;

        public XElement getXElement()
        {
            XElement e = new XElement("FileInfo");
            e.SetAttributeValue("Name", name);
            e.SetAttributeValue("Path", path);
            e.SetAttributeValue("MD5", md5);
            e.SetAttributeValue("Size", size);
            return e;
        }

        public void fromXElement(XElement e)
        {
            if (e.Name != "FileInfo")
            {
                throw new Exception();
            }

            name = e.Attribute("Name").ToString();
            path = e.Attribute("Path").ToString();
            md5 = e.Attribute("MD5").ToString();
            size = Convert.ToInt64( e.Attribute("Size").ToString());
        }
    }
}
