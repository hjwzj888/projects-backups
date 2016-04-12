using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using System.Collections;

namespace SPL_OPT_with_IBEAs
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
    }

    //---CLASS---   软件产品线类
    public class SPL
    {
        ArrayList aTerminatePoint;//用来生成时使用
        FeaturePoint rootPoint;
        public ArrayList aFeaturePoint;
        public ArrayList aConstraint;
        public int maxFeature;
        public int maxConstraint;

        //---CLASS---   特征点类
        public class FeaturePoint
        {
            public enum Relation { And, Or, Alternative };

            static int featureCount;
            static int maxFeature;
            string name;
            public FeaturePoint parent;
            public FeaturePoint[] children;
            int childrenCount;
            public bool IsMandatory;
            Relation childrenRelation;
            int maxPrice = 500;
            int maxPerformance = 500;

            public bool isChosen = false;

            public int price;
            public int performance;

            public FeaturePoint(FeaturePoint parent)
            {
                featureCount++;
                IsMandatory = false;
                price = 0;
                performance = 0;

                if (parent == null)//这意味着这是根节点
                {
                    IsMandatory = true;
                    SetName("Root");
                }

                this.parent = parent;
            }

            public bool CheckValidity()
            {
                //如果是强制点但却没选,则错
                if (IsMandatory == true && isChosen == false && (parent == null || parent.isChosen == true))
                {
                    return false;
                }

                //如果自己选了，如果有父节点却没选，则错
                if (isChosen == true && parent != null && parent.isChosen == false)
                {
                    return false;
                }

                //如果是叶子节点, 则一定正确
                if(children == null)
                {
                    return true;
                }

                //只有在自己被选中的情况下，才考虑孩子是不是合法
                if (isChosen == true)
                {
                    int chosenChildren = 0;
                    foreach (FeaturePoint child in children)
                    {
                        //如果出现孩子选了
                        if (child.isChosen == true)
                        {
                            chosenChildren++;
                        }
                    }
                    switch (childrenRelation)
                    {
                        //只能选一个，否则就错
                        case Relation.Alternative:
                            {
                                if (chosenChildren != 1)
                                {
                                    return false;
                                }
                                break;
                            }

                        //至少选一个，不然则错
                        case Relation.Or:
                            {
                                if (chosenChildren < 1)
                                {
                                    return false;
                                }
                                break;
                            }

                        //因为And的情况中，所有其他点预订是Mandatory的，所以在这里不用进行特别的考虑
                        case Relation.And:
                            {
                                break;
                            }
                    }
                }
                //经过重重考验,终于正确
                return true;
            }

            //序号0：没有问题
            //序号1: 强制节点没有选择 -- 那就选
            //序号2：自己选择，父节点却没有选 -- 那就选上父亲
            //序号3: Alternative关系出错 -- 那就把孩子全部放弃，挑一个选
            //序号4：Or关系出错 -- 那就把孩子全部放弃，挑几个选

            public int GetErrorNumber()
            {
                //一旦父母被选中（或者没有父节点，即自己是根节点）如果是强制点但却没选,则错
                if (IsMandatory == true && isChosen == false && (parent == null || parent.isChosen == true))
                {
                    return 1;
                }

                //如果自己选了，如果有父节点却没选，则错
                if (isChosen == true && parent != null && parent.isChosen == false)
                {
                    return 2;
                }

                //没孩子肯定是正确的啦
                if (children == null)
                {
                    return 0;
                }

                //只有在自己被选中的情况下，才考虑孩子是不是合法
                if (isChosen == true)
                {
                    int chosenChildren = 0;
                    foreach (FeaturePoint child in children)
                    {
                        //如果出现孩子选了
                        if (child.isChosen == true)
                        {
                            chosenChildren++;
                        }
                    }
                    switch (childrenRelation)
                    {
                        //只能选一个，否则就错
                        case Relation.Alternative:
                            {
                                if (chosenChildren != 1)
                                {
                                    return 3;
                                }
                                break;
                            }

                        //至少选一个，不然则错
                        case Relation.Or:
                            {
                                if (chosenChildren < 1)
                                {
                                    return 4;
                                }
                                break;
                            }
                    }
                }
                //经过重重考验,终于正确
                return 0;
            }

            public void Epoch()
            {
                if (featureCount >= maxFeature)
                {
                    return;
                }
                Random random = new Random();
                System.Threading.Thread.Sleep(500);

                //一旦生了孩子，自己就不是叶子节点，也就没有price和performance了
                price = 0;
                performance = 0;

                //每种子节点关系各有1/3概率被选中 
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


                childrenCount = random.Next(1, 5);//假设孩子数量为1-5
                children = new FeaturePoint[childrenCount];

                Console.Write("现在生成的是" + name + "的孩子，它们之间的关系为：");
                switch (childrenRelation)
                {
                    case Relation.Alternative:
                        Console.WriteLine("Alternative");
                        break;

                    case Relation.Or:
                        Console.WriteLine("Or");
                        break;

                    case Relation.And:
                        Console.WriteLine("And");
                        break;
                }
                for (int i = 0; i < childrenCount; i++)
                {
                    children[i] = new FeaturePoint(this);
                    children[i].price = random.Next(0, maxPrice) + 1;
                    children[i].performance = random.Next(0, maxPerformance) + 1;
                    children[i].SetName(name + "." + i);
                    Console.WriteLine("已生成节点，该节点为:" + children[i].name + " Price:" + children[i].price + " Performance:" + children[i].performance);
                }

                //如果是“与”关系，则可能有强制节点与可选节点的区别
                if (childrenRelation == Relation.And)
                {
                    for (int i = 0; i < childrenCount; i++)
                    {
                        if (random.NextDouble() < 0.7)
                        {
                            children[i].IsMandatory = true;
                            Console.WriteLine(children[i].name + "是强制节点");
                        }
                    }
                }
            }

            public void Init(int max)
            {
                featureCount = 0;
                maxFeature = max;
            }

            public bool IsFull()
            {
                if (featureCount < maxFeature)
                    return false;
                else
                {
                    return true;
                }
            }

            public int GetFeatureCount()
            {
                return featureCount;
            }

            public void SetName(string name)
            {
                this.name = name;
            }

            public string GetName()
            {
                return name;
            }

            public void SetChildrenRelation(Relation relation)
            {
                childrenRelation = relation;
            }

            public FeaturePoint[] GetChildren()
            {
                return children;
            }
        }

        //---CLASS---   约束条件类
        public class Constraint
        {
            public enum relation { implied, excluded };

            public relation constraintRealtion;
            public int indexA;//第一个特征点在所有特征点中的索引
            public int indexB;//第二个特征点在所有特征点中的索引

        }

        //构造函数
        public SPL()
        {
            aTerminatePoint = new ArrayList();
            aFeaturePoint = new ArrayList();
            aConstraint = new ArrayList();
        }

        //随机生成一个特征模型
        public void RandomlyGenerate()
        {
            Random random = new Random();

            rootPoint = new FeaturePoint(null);
            rootPoint.price = 0;
            rootPoint.performance = 0;
            rootPoint.Init(maxFeature);
            aTerminatePoint.Add(rootPoint);
            aFeaturePoint.Add(rootPoint);

           

            while (rootPoint.IsFull() == false)
            {
                if (aTerminatePoint.Count == 0)
                {
                    MessageBox.Show("队列已经空掉啦 到此为止咯");
                    return;
                }

                //随机选择一个叶子节点
                int i = random.Next(0, aTerminatePoint.Count);

                //将其复制给临时节点后,将其从队列里移除
                FeaturePoint t = (FeaturePoint)aTerminatePoint[i];
                aTerminatePoint.RemoveAt(i);

                t.Epoch();
                foreach (FeaturePoint child in t.GetChildren())
                {
                    aTerminatePoint.Add(child);
                    aFeaturePoint.Add(child);
                }
            }
        }

        //随机生成约束条件
        //为了避免约束条件出现环,暂时定义只有排序靠前的点可以对后面的点进行约束
        public void RandomConstraint()
        {
            Random random = new Random();
            for (int i = 0; i < maxConstraint; i++)
            {
                Constraint constraint = new Constraint();
                int a = random.Next(0, aFeaturePoint.Count - 1);//至少留出一个空位给b, 所以是aFeaturePoint.Count - 1
                int b;
                do
                {
                    b = random.Next(a, aFeaturePoint.Count);
                } while (b == a);
                if (random.NextDouble() < 0.5)
                {
                    constraint.constraintRealtion = Constraint.relation.implied;
                }
                else
                {
                    constraint.constraintRealtion = Constraint.relation.excluded;
                }
                constraint.indexA = a;
                constraint.indexB = b;
                aConstraint.Add(constraint);
                FeaturePoint ta = (FeaturePoint)aFeaturePoint[a];
                FeaturePoint tb = (FeaturePoint)aFeaturePoint[b];
                Console.WriteLine(ta.GetName() + " " + constraint.constraintRealtion.ToString() + " " + tb.GetName());
            }
        }

        //返回样本模型
        public void GenerateSample()
        {
            FeaturePoint F0 = new FeaturePoint(null);
            F0.SetName("F0");
            F0.SetChildrenRelation(FeaturePoint.Relation.Or);
            F0.IsMandatory = true;

            FeaturePoint F1 = new FeaturePoint(F0);
            F1.SetName("F1");
            F1.SetChildrenRelation(FeaturePoint.Relation.Or);
            F1.IsMandatory = true;

            FeaturePoint F2 = new FeaturePoint(F0);
            F2.SetName("F2");
            F2.price = 100;
            F2.performance = 100;

            FeaturePoint F3 = new FeaturePoint(F1);
            F3.SetName("F3");
            F3.SetChildrenRelation(FeaturePoint.Relation.And);

            FeaturePoint F4 = new FeaturePoint(F1);
            F4.SetName("F4");
            F4.price = 200;
            F4.performance = 180;

            FeaturePoint F5 = new FeaturePoint(F1);
            F5.SetName("F5");
            F5.SetChildrenRelation(FeaturePoint.Relation.Alternative);

            FeaturePoint F6 = new FeaturePoint(F3);
            F6.IsMandatory = true;
            F6.SetName("F6");
            F6.price = 150;
            F6.performance = 75;

            FeaturePoint F7 = new FeaturePoint(F3);
            F7.IsMandatory = true;
            F7.SetName("F7");
            F7.price = 35;
            F7.performance = 40;

            FeaturePoint F8 = new FeaturePoint(F5);
            F8.SetName("F8");
            F8.price = 75;
            F8.performance = 80;

            FeaturePoint F9 = new FeaturePoint(F5);
            F9.SetName("F9");
            F9.price = 50;
            F9.performance = 90;

            F0.children = new FeaturePoint[2];        
            F0.children[0] = F1;
            F0.children[1] = F2;

            F1.children = new FeaturePoint[3];
            F1.children[0] = F3;
            F1.children[1] = F4;
            F1.children[2] = F5;

            F3.children = new FeaturePoint[2];
            F3.children[0] = F6;
            F3.children[1] = F7;

            F5.children = new FeaturePoint[2];
            F5.children[0] = F8;
            F5.children[1] = F9;

            aFeaturePoint.Add(F0);
            aFeaturePoint.Add(F1);
            aFeaturePoint.Add(F2);
            aFeaturePoint.Add(F3);
            aFeaturePoint.Add(F4);
            aFeaturePoint.Add(F5);
            aFeaturePoint.Add(F6);
            aFeaturePoint.Add(F7);
            aFeaturePoint.Add(F8);
            aFeaturePoint.Add(F9);

            Constraint c1 = new Constraint();
            c1.indexA = 4;
            c1.indexB = 6;
            c1.constraintRealtion = Constraint.relation.implied;
            aConstraint.Add(c1);

            Constraint c2 = new Constraint();
            c2.indexA = 7;
            c2.indexB = 8;
            c2.constraintRealtion = Constraint.relation.excluded;
            aConstraint.Add(c2);
        }
    }

    public class PassValuesEventArgs : EventArgs
    {
        public SPL spl { get; set; }
        public PassValuesEventArgs(SPL spl)
        {
            this.spl = spl;
        }
    }
}
