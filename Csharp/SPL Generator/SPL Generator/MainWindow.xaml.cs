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

using System.Collections;

namespace SPL_Generator
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        SPL spl;

        public MainWindow()
        {
            InitializeComponent();          
        }

        //---CLASS---   软件产品线类
        class SPL
        {
            ArrayList aTerminatePoint;
            FeaturePoint rootPoint;

            //---CLASS---   特征点类
            class FeaturePoint
            {
                enum Relation { And, Or, Alternative };

                static int featureCount = 0;
                static int maxFeature = 20;
                string name;
                FeaturePoint parent;
                FeaturePoint[] children;
                int childrenCount;
                bool IsMandatory;
                Relation childrenRelation;

                public FeaturePoint(FeaturePoint parent)
                {
                    featureCount++;
                    IsMandatory = false;
                    
                    if (parent == null)//这意味着这是根节点
                    {
                        IsMandatory = true;
                        name = "Root";
                    }
                }

                public void Epoch()
                {
                    if (featureCount >= maxFeature)
                    {
                        return;
                    }

                    Random random = new Random();
                    System.Threading.Thread.Sleep(500);

                    //如果没有强制节点，则每种子节点关系各有1/3概率被选中 
                    if (random.NextDouble() < 0.3)
                    {
                        childrenRelation = Relation.Or;
                    }
                    else if (random.NextDouble() < 0.5)
                    {
                        childrenRelation = Relation.And;
                    }
                    else
                    {
                        childrenRelation = Relation.Alternative;
                    }

                    childrenCount = random.Next(1,5);//假设孩子数量为1-5
                    children = new FeaturePoint[childrenCount];

                    for (int i = 0; i < childrenCount; i++)
                    {
                        children[i] = new FeaturePoint(this);
                        children[i].SetName(name + "." + i);

                        //如果有强制节点，则该节点必须被选择，且子节点关系一定为“或”
                        if (random.NextDouble() < 0.1)
                        {
                            children[i].IsMandatory = true;
                            childrenRelation = Relation.Or;
                        }
                    }
                }

                public void SetName(string name)
                {
                    this.name = name;
                    Console.WriteLine("已生成节点，该节点为:" + name);
                }

                public FeaturePoint[] GetChildren()
                {
                    return children;
                }

                public void Init()
                {
                    featureCount = 0;
                }

                public bool IsFull()
                {
                    if (featureCount < maxFeature)
                        return false;
                    else
                        return true;
                }
            }

            //构造函数
            public SPL()
            {
                aTerminatePoint = new ArrayList();
            }

            public void RandomlyGenerate()
            {
                rootPoint = new FeaturePoint(null);
                rootPoint.Init();
                aTerminatePoint.Add(rootPoint);

                Random random = new Random();

                while (rootPoint.IsFull() == false)
                {
                    //随机选择一个叶子节点
                    int i = random.Next(0, aTerminatePoint.Count);
                    if (i == 0)
                    {
                        MessageBox.Show("队列已经空掉啦 到此为止咯");
                        return;
                    }

                    //将其复制给临时节点后,将其从队列里移除
                    FeaturePoint t = (FeaturePoint)aTerminatePoint[i];
                    aTerminatePoint.RemoveAt(i);

                    t.Epoch();
                    foreach (FeaturePoint child in t.GetChildren())
                    {
                        aTerminatePoint.Add(child);
                    }
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            spl = new SPL();
            spl.RandomlyGenerate();
        }
    }
}
