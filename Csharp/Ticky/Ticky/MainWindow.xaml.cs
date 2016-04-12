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
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.IO;
using System.Threading;
using System.Diagnostics;
//using System.Windows.Forms;

namespace Ticky
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        //The main timer. Refresh every second.
        DispatcherTimer dispatcherTimer; 
  
        string dateTime;
        string textContent;
        public MainWindow()
        {
            InitializeComponent();
            this.ShowInTaskbar = false;
            dispatcherTimer  = new DispatcherTimer();   //Initialize main timer
            dispatcherTimer.Interval = TimeSpan.FromSeconds(0.1);
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Start();
        }

        void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            //Use files as a easy way to realize data transformation between windows
            LoadData();
            text.Content = textContent;

            DateTime dtDesigned = DateTime.Parse(dateTime);
            DateTime dtNow = DateTime.Now;

            TimeSpan timeDeviation = dtDesigned - dtNow;

            //Determine whether time deviation is negative
            if (timeDeviation.Seconds < 0)
            {
                timeDeviation = timeDeviation.Negate();
                label1.Content = "已过";
            }
            else
                label1.Content = "还有";

            dayLabel.Content = timeDeviation.Days.ToString();
            hourLabel.Content = timeDeviation.Hours.ToString();
            minuteLabel.Content = timeDeviation.Minutes.ToString();
            secondLabel.Content = timeDeviation.Seconds.ToString();
            
        }

        //---   Sundries
        #region  
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

        //---   Exit button
        private void Exit_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        //---   Setting button
        private void Setting_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            setting settingWindow = new setting();
            settingWindow.ShowDialog();
        }

        //---   Fade in setting button and exit button
        private void Canvas_MouseEnter(object sender, MouseEventArgs e)
        {
            DoubleAnimation rtOpacityAnimation = new DoubleAnimation();
            rtOpacityAnimation.From = setting_button.Opacity;
            rtOpacityAnimation.To = 1;
            rtOpacityAnimation.Duration = TimeSpan.FromSeconds(0.5);
            rtOpacityAnimation.AccelerationRatio = 1;
            setting_button.BeginAnimation(Rectangle.OpacityProperty, rtOpacityAnimation);
            exit_button.BeginAnimation(Rectangle.OpacityProperty, rtOpacityAnimation);  //Since the two buttons fade in at same time , I apply only one animation to them
        }

        //---   Fade out setting button and exit button
        private void Canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            DoubleAnimation rtOpacityAnimation = new DoubleAnimation();
            rtOpacityAnimation.From = setting_button.Opacity;
            rtOpacityAnimation.To = 0;
            rtOpacityAnimation.Duration = TimeSpan.FromSeconds(0.5);
            rtOpacityAnimation.AccelerationRatio = 1;
            setting_button.BeginAnimation(Rectangle.OpacityProperty, rtOpacityAnimation);
            exit_button.BeginAnimation(Rectangle.OpacityProperty, rtOpacityAnimation);  //The same.
        }

        //---   Load data from files
        private void LoadData()
        {
            //read data by lines, using strLine to store each line
            string strLine;
            try
            {
                //Initialize file stream and stream reader
                FileStream aFile = new FileStream("data.dat", FileMode.Open);
                StreamReader sr = new StreamReader(aFile,System.Text.Encoding.GetEncoding("gb2312"));
                strLine = sr.ReadLine();
                textContent = strLine;
                strLine = sr.ReadLine();
                dateTime = strLine;
                strLine = sr.ReadLine();
                if (strLine == "True")
                    logo.Opacity = 1;
                else
                    logo.Opacity = 0;
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
    
    }//main class ends here
}//namespace ends here
#endregion