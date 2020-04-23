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
using System.Windows.Shapes;

namespace MCUclient
{
    /// <summary>
    /// Логика взаимодействия для ExcludeFiles.xaml
    /// </summary>
    public partial class ExcludeFiles : Window
    {
        public ExcludeFiles()
        {
            InitializeComponent();
        }


        public string[] files
        {
            set
            {
                tbFiles.Text = string.Empty;
                foreach (string s in value)
                    tbFiles.Text += s + Environment.NewLine;
            }
            get
            {
                string[] slist = tbFiles.Text.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                return slist;
            }
        }
    }
}
