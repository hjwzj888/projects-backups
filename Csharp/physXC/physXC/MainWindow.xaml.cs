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
using System.Windows.Threading;

namespace physXC
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        physX phys_Ball;
        public MainWindow()
        {
            InitializeComponent();
            phys_Ball = new physX(ref rect,ref carrier);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Canvas.SetLeft(rect,82);
            Canvas.SetTop(rect, 86);
            phys_Ball.GetParams(SpeedSlider,AngleSlider);
            phys_Ball.start();
        }

        class physX
        {
            private Canvas carrier;
            private Shape obj;
            private double velocityX = 0;
            private double velocityY = 0;
            private double gravity = 9.8;
            //x pixel per meter
            private double scale = 50;
            //refresh every x milliseconds
            private double refreshFrequency = 10;
            //efficient coefficient
            private double efficiency = 0.7; 
            private DispatcherTimer RULE = new DispatcherTimer();

            public physX(ref Rectangle s,ref Canvas c)
            {
                carrier = c;
                obj = s;
                RULE.Interval = TimeSpan.FromMilliseconds(refreshFrequency);
                RULE.Tick += RULE_Tick;
            }

            void RULE_Tick(object sender, EventArgs e)
            {
                velocityY -= gravity * (refreshFrequency / 1000);
                double X = Canvas.GetLeft(obj);
                double Y = Canvas.GetTop(obj);
                X += velocityX * scale * (refreshFrequency / 1000);
                //Only when the obj was touching the bottom line from above
                if (((Y + obj.Height) - 200) > 0.0000001 && velocityY < 0) 
                {
                    velocityX = velocityX * efficiency;
                    velocityY = -velocityY * efficiency;
                }
                Y -= velocityY * scale * (refreshFrequency / 1000);
                Canvas.SetLeft(obj,X);
                Canvas.SetTop(obj,Y);
            }

            public void start()
            {
                RULE.Start();
            }

            public void GetParams(Slider speedslider,Slider angleslider)
            {
                velocityX = speedslider.Value * Math.Cos(angleslider.Value * Math.PI / 180);
                velocityY = speedslider.Value * Math.Sin(angleslider.Value * Math.PI / 180);
            }
        }//class phsyX ends here
    }
}
