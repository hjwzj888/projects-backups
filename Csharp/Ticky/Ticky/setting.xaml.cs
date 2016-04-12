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
using System.IO;

namespace Ticky
{
    /// <summary>
    /// setting.xaml 的交互逻辑
    /// </summary>
    public partial class setting : Window
    {
        public setting()
        {
            InitializeComponent();
            LoadData();
            ShowInTaskbar = false;
        }
        
        //---   Drag window
        private void Drag_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                this.DragMove();
            }
            catch
            {
                //Catch drag errors
            }
        }

        //---   Close window and save not changes
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        //---   Save changes and close the window
        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            SaveData();
            this.Close();
        }

        //---   Save changes
        private void SaveData()
        {
            try
            {
                FileStream aFile = new FileStream("data.dat", FileMode.Open);
                StreamWriter sw = new StreamWriter(aFile,System.Text.Encoding.GetEncoding("gb2312"));
                sw.WriteLine(contentBox.Text);
                sw.WriteLine(dateBox.Text);
                sw.WriteLine(checkBox1.IsChecked);
                sw.Close();
            }
            catch (IOException ex)
            {
                MessageBox.Show("保存數據失敗了!");
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
                return;
            }
        }

        //---   Load data and refresh the existing text
        private void LoadData()
        {
            string strLine;
            try
            {
                //Initialize file stream and stream reader
                FileStream aFile = new FileStream("data.dat", FileMode.Open);
                StreamReader sr = new StreamReader(aFile, System.Text.Encoding.GetEncoding("gb2312"));
                strLine = sr.ReadLine();
                contentBox.Text = strLine;
                strLine = sr.ReadLine();
                dateBox.Text = strLine;
                strLine=sr.ReadLine();
                if (strLine == "True")
                    checkBox1.IsChecked = true;
                else
                    checkBox1.IsChecked = false;
                //close stream reader
                sr.Close();
            }
            catch (IOException ex)
            {
                MessageBox.Show("配置文件打開失敗");
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
                return;
            } 
        }

    }
}
