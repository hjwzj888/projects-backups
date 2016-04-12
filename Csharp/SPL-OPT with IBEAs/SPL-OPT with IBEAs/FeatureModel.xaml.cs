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

using System.Collections;

namespace SPL_OPT_with_IBEAs
{
    /// <summary>
    /// FeatureModel.xaml 的交互逻辑
    /// </summary>
    public partial class FeatureModel : Window
    {
        SPL spl;

        public delegate void PassValuesHandler(object sender, PassValuesEventArgs e);

        public event PassValuesHandler PassValuesEvent;

        public FeatureModel()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("开始-生成. 布-里 ●▂● ");
            spl = new SPL();
            spl.maxFeature = int.Parse(max.Text);
            spl.maxConstraint = int.Parse(constraint.Text);
            spl.RandomlyGenerate();
            spl.RandomConstraint();

            MessageBox.Show("生成-完毕 布里布里◑▂◐\n共生成节点" + spl.aFeaturePoint.Count + "个\n约束条件" + spl.aConstraint.Count + "条.. 兹--");

            PassValuesEventArgs args = new PassValuesEventArgs(spl);
            PassValuesEvent(this,args);

            this.Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (spl != null)
            {
                PassValuesEventArgs args = new PassValuesEventArgs(spl);
                PassValuesEvent(this, args);
            }
          //  MessageBox.Show("谢谢使用.. 布里兹");
            this.Close();
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            spl = new SPL();
            spl.GenerateSample();

            MessageBox.Show("使用样本模型成功 ◑▂◐");

            PassValuesEventArgs args = new PassValuesEventArgs(spl);
            PassValuesEvent(this, args);

            this.Close();
        }
    }
}
