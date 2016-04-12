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

namespace SPL_OPT_with_IBEAs
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        double f1RangeFrom; 
        double f1RangeTo;
        double f2RangeFrom;
        double f2RangeTo;

        ArrayList array_ParetoFront = new ArrayList();

        ArrayList array_popP = new ArrayList();
        ArrayList array_popQ = new ArrayList();

        SPL spl;

        IBEA ibea;
        IBEAEng ibeaEng;


        public MainWindow()
        {
            InitializeComponent();
            InitializeScreen();
        }
        //---CLASS--- ParetoPoint
        class ParetoPoint
        {
            public double X;
            public double f1;
            public double f2;

            //Constructor function
            public ParetoPoint(double X,double f1,double f2)
            {
                this.X = X;
                this.f1 = f1;
                this.f2 = f2;
            }
        }

        //---CLASS--- Indicator-based evolutionary algorithm
        class IBEA 
        {
            //---CLASS--- Genome
            // !!! Need to be rewritten when applied to another problem;
            public class Genome
            {
                static int length;
                public int[] aGene;// 0,1
                double fitness;

                //Constructor
                public Genome()
                {
                    aGene = new int[length];
                    fitness = 0;
                }

                public void Duplicate(Genome anotherGenome)
                {
                    anotherGenome.aGene.CopyTo(aGene, 0);
                    SetFitness(anotherGenome.GetFitness());
                }

                public void SetFitness(double fitness) 
                {
                    this.fitness = fitness;
                }

                public double GetFitness()
                {
                    return fitness;
                }

                public int GetLength()
                {
                    return length;
                }

                public void SetLength(int l)
                {
                    length = l ;
                }
            }

            //---CLASS--- Population 
            public class Population
            {
                public int size;
                public Genome[] aGenome;

                //Constructor 
                public Population(int popSize)
                {
                    size = popSize;
                    aGenome = new Genome[popSize];  
                }
            }

            //最佳indicator
            double maxIndicator;

            //最差indicator
            double minIndicator;

            //總適應度
            double totalFitness;

            //最佳適應度
            double bestFitness;

            //平均適應度
            double averageFitness;

            //最差適應度
            double worstFitness;

            //最佳基因
            Genome fittnestGenome;

            //變異概率, 一般為0.05-0.3
            double mutationRate;

            //交叉概率,一般為0.7
            double crossoverRate;

            //進化代數計數器
            int generation;

            //最大进化代数
            int maxGeneration;

            //最大變異步長
            double maxPerturbation;

            double leftPoint;

            double rightPoint;


            //交配池 种群Q
            Population popQ;

            //档案 种群P
            Population popP;

            //集合 种群R
            Population popR;

            //种群P的规模
            int PSize;

            //种群Q的规模
            int QSize;

            //种群R的规模
            int RSize;

            //用来存放最后的Pareto前沿
            ArrayList aParetoFront;

            //参考点
            Point referencePoint = new Point(20,20);

            //产品线
            SPL spl;

            Random random = new Random();

             //纯算法部分
            #region
            //構造函數
            public IBEA() 
            {

            }

            // Initialize parameters like popSize,chromoLength
            public void Init(double mutationRate, double crossoverRate,double leftPoint, 
                double rightPoint,int PSize, int QSize,int maxGeneration,SPL spl,int GenomeLength,int preciseLevel = 0)
            {
                this.mutationRate = mutationRate;
                this.crossoverRate = crossoverRate;
                this.leftPoint = leftPoint;
                this.rightPoint = rightPoint;
                this.PSize = PSize;
                this.QSize = QSize;
                this.maxGeneration = maxGeneration;
                this.spl = spl;

                //Calculate the length of each genome
                //int i = 0;
                //while (Math.Pow(2, i) < (rightPoint - leftPoint) * Math.Pow(10, preciseLevel))
                //{
                //    i++;
                //}

                Genome g = new Genome();
                g.SetLength(GenomeLength);
                Console.WriteLine("初始化完成，染色体编码共{0}位",GenomeLength);

                RSize = PSize + QSize;

                popP = new Population(PSize);
                popQ = new Population(QSize);
                popR = new Population(RSize);

                RandomlyGenerate(ref popQ);
                //ExcludeTheSame(ref popQ);
                ExcludeTheIllegal(ref popQ);

                RandomlyGenerate(ref popP);
                //ExcludeTheSame(ref popP);
                ExcludeTheIllegal(ref popP);

                generation = 0;

                bestFitness = 0;
                worstFitness = 99999999;
                averageFitness = 0;

                maxIndicator = 999999999999;
                minIndicator = 0;

               
            }

            // Code x into a Genome
            // !!! Need to be rewritten when apply to another problem;
            public void Code(ref Genome genome,double x)
            {
                int xt;
                xt = (int)((x - leftPoint) / (rightPoint - leftPoint) * (Math.Pow(2, genome.GetLength()) - 1));

                int weight = 2;
                for (int i = genome.GetLength() - 1; i >= 0; i--)
                {
                    genome.aGene[i] = xt % 2;
                    weight *= 2;
                    xt /= 2;
                }
            }

            // Decode x from a Genome
            // !!! Need to be rewritten when apply to another problem;
            public double Decode(Genome genome)
            {
                double x;

                //Convert coded gene from binary to decimal
                double xt = 0;
                int weight = 1;

                for (int i = genome.GetLength() - 1; i >=0; i--)
                {
                    xt += genome.aGene[i] * weight;
                    weight *= 2;
                }

                //Then convert xt to the real x in the interval
                x = leftPoint + xt / (Math.Pow(2, genome.GetLength()) - 1) * (rightPoint - leftPoint);

                return x;
            }

            //Sort one genome array,with the best genome at the end 
            public void Sort(Genome[] aGenome)
            {
                for (int i = 0; i < aGenome.Length-1; i++)
                {
                    for (int j = i+1; j < aGenome.Length; j++)
                    {
                        if (aGenome[i].GetFitness() > aGenome[j].GetFitness())
                        {
                            Genome t = aGenome[i];
                            aGenome[i] = aGenome[j];
                            aGenome[j] = t;
                        }
                    }
                }
            }

            // Mutate
            // Randomly choose one Gene Position then revert it
            public void Mutate(ref Genome genome)
            {
                //for (int i = 0; i < genome.GetLength(); i++)
                //{
                //    genome.aGene[i] = (genome.aGene[i] + 1) % 2;
                //}
                int rd = random.Next(0, genome.GetLength());
                genome.aGene[rd] = (genome.aGene[rd] + 1) % 2;
            }

            //重载的变异函数，通过设定变异位，以避免多次随机出同样数字的问题
            public void Mutate(ref Genome genome,int rd)
            {
                //for (int i = 0; i < genome.GetLength(); i++)
                //{
                //    genome.aGene[i] = (genome.aGene[i] + 1) % 2;
                //}
                genome.aGene[rd] = (genome.aGene[rd] + 1) % 2;
            }

            // Crossover
            // one-point crossover
            public void Crossover(ref Genome genome1,ref Genome genome2)
            {
                int rd = random.Next(0, genome1.GetLength());
                double p = random.Next(0,1);
                if (p < 0.5)
                {
                    for (int i = 0; i < rd; i++)
                    {
                        int t = genome1.aGene[i];
                        genome1.aGene[i] = genome2.aGene[i];
                        genome2.aGene[i] = t;

                    }
                }
                else
                {
                    for (int i = rd + 1; i < genome1.GetLength(); i++)
                    {
                        int t = genome1.aGene[i];
                        genome1.aGene[i] = genome2.aGene[i];
                        genome2.aGene[i] = t;
                    }
                }
                //System.Threading.Thread.Sleep(5);
            }

            //重载的交叉函数，通过设定交叉位，以避免多次随机出同样数字的问题
            public void Crossover(ref Genome genome1, ref Genome genome2,int rd,double p)
            {
                if (p < 0.5)
                {
                    for (int i = 0; i < rd; i++)
                    {
                        int t = genome1.aGene[i];
                        genome1.aGene[i] = genome2.aGene[i];
                        genome2.aGene[i] = t;

                    }
                }
                else
                {
                    for (int i = rd + 1; i < genome1.GetLength(); i++)
                    {
                        int t = genome1.aGene[i];
                        genome1.aGene[i] = genome2.aGene[i];
                        genome2.aGene[i] = t;
                    }
                }
            }

            // Get the best N Genomes from a population ,using Championship algorithm
            public Genome[] GetBestGenomes(Population pop,int N) 
            {
                //Make a copy of pop.aGenome,in case of references disaster;
                Genome[] aGenome = new Genome[pop.size];
                for (int i = 0; i < aGenome.Length; i++)
                {
                    aGenome[i] = new Genome();
                    aGenome[i].Duplicate(pop.aGenome[i]);
                }


                if (N >= pop.size)
                {
                    return aGenome;
                }

                Genome[] bestGenomes = new Genome[N];

                //All the moves are done in this arraylist             
                ArrayList temp = new ArrayList();

                //The number of how many genomes will be put into the championship
                int chamSize = pop.size/2 ;

                Genome[] arena = new Genome[chamSize];

                Random random = new Random();

                foreach (Genome genome in aGenome)
                {
                    temp.Add(genome);
                }

                for (int i = 0; i < N; i++)
                {
                    for (int j = 0; j < chamSize; j++)
                    {
                        int rd = random.Next(0,temp.Count);

                        //Move the randomly choosen genome into the arena
                        arena[j] = (Genome)temp[rd];

                        //Then remove it
                        temp.RemoveAt(rd);
                    }
                    //Now we got all the champions in the arena. Let's sort them.
                    Sort(arena);

                    bestGenomes[i] = arena[chamSize - 1];

                    for (int k = 0; k < chamSize - 1; k++)
                    {
                        temp.Add(arena[k]);
                    }
                }
                return bestGenomes;
            }

            // Calculate fitness of a genome
            public double CalculateFitness(Genome genome)
            {
                //如果不合法,那么这就是最差的解,有很大概率被排除
                if (IsLegal(genome) == false)
                {
                    return -1;
                }

                Double hyperVolume;

              //  Double x = Decode(genome);
                Double f1 = optFunc1(genome);
                Double f2 = optFunc2(genome);

                hyperVolume = (referencePoint.X - f1) * (f2 - referencePoint.Y);

                return hyperVolume;
            }

            // Modify fitness
            public void ModifyFitness(ref Population pop)
            {
                foreach (Genome genome in pop.aGenome)
                {
                    if (genome.GetFitness() > 0)
                    {
                        double tempFitness = genome.GetFitness();
                        genome.SetFitness(Math.Sqrt(2 * (tempFitness - minIndicator) / (maxIndicator - minIndicator)));
                    }
                }
            }

            // Calculate fitness of a whole population
            public void CalculateHyperVolumeContribution(ref Population pop)
            {
                for (int i = 0; i < pop.size; i++)
                {
                    double worstF1 = 99999999;//右上方,横坐标最小
                    double worstF2 = 0;//左下方,纵坐标最大
                    Genome x1 = pop.aGenome[i];
                    for (int j = 0; j < pop.size; j++)
                    {
                        //如果I和J相等, 或者任意一个已经被支配(也就是适应度为0), 那么就可以跳过了
                        if (i == j)
                            continue;
                        Genome x2 = pop.aGenome[j];

                        //如果X2在X1的上方
                        if (optFunc2(x2) >= optFunc2(x1))
                        {
                            //X2在X1的左上, 证明被支配,X1的适应度为0
                            if (optFunc1(x2) < optFunc1(x1))
                            {
                                pop.aGenome[i].SetFitness(-1);
                                break;
                            }
                            //X2在右上,那么就要找到横坐标最小的点
                            else
                            {
                                if(optFunc1(x2)<worstF1)
                                {
                                    worstF1 = optFunc1(x2);
                                }
                            }
                        }
                        //反之,如果X2在X1的下方
                        else
                        {
                            //X2在X1的左下方 , 就要找到纵坐标最大的点
                            if (optFunc1(x2) <= optFunc1(x1))
                            {
                                if(optFunc2(x2)>worstF2)
                                {
                                    worstF2 = optFunc2(x2);
                                }
                            }
                            //X2在X1的右下,X2被支配,所以适应度设为0
                            else
                            {
                                pop.aGenome[j].SetFitness(-1);
                            }
                        }
                    }
                    double hyperVolunmeContribution = (worstF1 - optFunc1(x1)) * (optFunc2(x1) - worstF2);

                    //更新maxIndicator和minIndicator
                    if (hyperVolunmeContribution > maxIndicator)
                    {
                        maxIndicator = hyperVolunmeContribution;
                    }
                    if (hyperVolunmeContribution < minIndicator)
                    {
                        minIndicator = hyperVolunmeContribution;
                    }

                    //只有当i未被支配时，才采用这个算法对其进行适应度分配
                    if (pop.aGenome[i].GetFitness() >= 0)
                    {
                        pop.aGenome[i].SetFitness(hyperVolunmeContribution);
                    }
                }
            }

            // Exclude the same genome for diversity
            public void ExcludeTheSame(ref Population pop) 
            {
                for (int i = 1; i < pop.size; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        if (Decode(pop.aGenome[i]) == Decode(pop.aGenome[j]))
                        {
                            pop.aGenome[i] = new Genome();
                            do
                            {
                                for (int k = 0; k < pop.aGenome[i].GetLength(); k++)
                                {
                                    pop.aGenome[i].aGene[k] = (int)random.Next(0, 2);
                                }
                                TransToLegal(ref pop.aGenome[i]);
                            } while (IsLegal(pop.aGenome[i]) == false);
                            //Mutate(ref pop.aGenome[i]);
                        }
                    }
                }
            }

            // Exclude the illegal genome
            public void ExcludeTheIllegal(ref Population pop)
            {
                for (int i = 1; i < pop.size; i++)
                {
                    if (IsLegal(pop.aGenome[i]) == false)
                    {
                        do
                        {
                            for (int j = 0; j < pop.aGenome[i].GetLength(); j++)
                            {
                                pop.aGenome[i].aGene[j] = (int)random.Next(0, 2);
                            }
                            TransToLegal(ref pop.aGenome[i]);
                        } while (IsLegal(pop.aGenome[i]) == false); 
                    }
                }
            }

            //Construct reference point using the extreme objective values found in R
            public void ConstructReference() 
            {
                double maxF1 = 0;
                double minF2 = 0;
                foreach(Genome genome in popR.aGenome)
                {
                    if (optFunc1(genome) > maxF1) 
                    {
                        maxF1 = optFunc1(genome); 
                    }

                    if (optFunc2(genome) < minF2) 
                    {
                        minF2 = optFunc1(genome);
                    }
                }
                referencePoint.X = maxF1 * 2;
                referencePoint.Y = minF2 / 2;
            }

            // Get next generation
            public void Epoch()
            {
                //Reset the size of R
                popR.size = RSize;

                generation++;

                // Step 1: Merge P and Q into R
                popP.aGenome.CopyTo(popR.aGenome,0);
                popQ.aGenome.CopyTo(popR.aGenome,popP.aGenome.Length);

                ExcludeTheSame(ref popR);
                ExcludeTheIllegal(ref popR);
                //ConstructReference();

                // Step 2: Calculate the fitness of genomes in R with the hypervolume indicator
                CalculateHyperVolumeContribution(ref popR);
                ModifyFitness(ref popR);

                // Step 3:Sort genomes in R. Delete the worst genome until RSize equals PSize.Then copy R to P.
                Sort(popR.aGenome);
                for (int i = 0; i < popP.size; i++)
                {
                    popP.aGenome[i] = popR.aGenome[popR.size -1 - i];
                }

                //Step 4: If genreration >= maxGeneration then output the genomes in P
                if (generation >= maxGeneration)
                {
                    aParetoFront = new ArrayList();
                    for (int i = 0; i < popP.size; i++)
                    {
                        //只将非劣解输出
                        if (popP.aGenome[i].GetFitness() >= 0)
                        {
                            aParetoFront.Add(popP.aGenome[i]);
                        }
                    }
                    return;
                }
                
                // Step 5: Copy the  best genemos from P into Q
                popQ.aGenome = GetBestGenomes(popP,popQ.size);

                // Step 6: Crossover and mutate genomes in Q
                Random random = new Random();
                //Console.WriteLine("Before:");
                //Console.WriteLine("{0}:", optFunc2(popQ.aGenome[0]));

                int[] park = new int[popQ.size];

                for (int i = 0; i < popQ.size; i++)
                {
                    if (random.NextDouble() < crossoverRate && park[i] == 0)
                    {
                        park[i] = 1;
                        int j = random.Next(0, popQ.size);
                        while (park[j] != 0)
                        {
                            j = random.Next(0, popQ.size);
                        }
                        park[j] = 1;

                        int rd = random.Next(0, popQ.aGenome[i].GetLength());
                        double p = random.NextDouble();
                        Crossover(ref popQ.aGenome[i], ref popQ.aGenome[j],rd,p);
                    }

                    if (random.NextDouble() < mutationRate)
                    {
                        int rd = random.Next(0,popQ.aGenome[i].GetLength());
                        Mutate(ref popQ.aGenome[i],rd);
                    }
                }
                //Console.WriteLine("After:");
                //Console.WriteLine("{0}:", optFunc2(popQ.aGenome[0]));
                //Console.WriteLine("");
            }

            // Generate the genomes in a population randomly
            private void RandomlyGenerate(ref Population pop)
            {
                int c = 0;
                for (int i = 0; i < pop.size; i++)
                {
                    pop.aGenome[i] = new Genome();
                    do
                    {
                        for (int j = 0; j < pop.aGenome[i].GetLength(); j++)
                        {
                            pop.aGenome[i].aGene[j] = (int)random.Next(0, 2);
                        }
                        TransToLegal(ref pop.aGenome[i]);
                        c++;
                    } while (IsLegal(pop.aGenome[i]) == false);
                }
                MessageBox.Show("为了这个群老娘跑了"+c+"次！");
            }

            // Get the arraylist of Pareto-Front
            public ArrayList GetParetoFront()
            {
                if (aParetoFront == null)
                {
                    return null;
                }
                else
                {
                    ArrayList a = new ArrayList();
                    foreach (Genome genome in aParetoFront)
                    {
                        Double x = Decode(genome);
                        ParetoPoint paretoPoint = new ParetoPoint(x, optFunc1(genome), optFunc2(genome));
                        a.Add(paretoPoint);
                    }
                    return a;
                }
            }

            // Get population p
            public ArrayList GetP()
            {
                ArrayList a = new ArrayList();
                for (int i = 0; i < popP.size; i++)
                {
                    Double x = Decode(popP.aGenome[i]);
                    ParetoPoint paretoPoint = new ParetoPoint(x, optFunc1(popP.aGenome[i]), optFunc2(popP.aGenome[i]));
                    a.Add(paretoPoint);
                }
                return a;
            }

            public ArrayList GetQ()
            {
                ArrayList a = new ArrayList();
                for (int i = 0; i < popQ.size; i++)
                {
                    if (IsLegal(popQ.aGenome[i]) == true)
                    {
                        Double x = Decode(popQ.aGenome[i]);
                        ParetoPoint paretoPoint = new ParetoPoint(x, optFunc1(popQ.aGenome[i]), optFunc2(popQ.aGenome[i]));
                        a.Add(paretoPoint);
                    }
                }
                Console.WriteLine("Generation {0}:{1}",generation,a.Count);
                return a;
            }

            // Optimization function 1  --- For SPL
            // !!! Need to be rewritten when apply to another problem;
            private double optFunc1(Genome genome)
            {
                int sumPrice = 0;

                for (int i = 0; i < genome.GetLength(); i++)
                {
                    if (genome.aGene[i] == 1)
                    {
                        SPL.FeaturePoint tempPoint = (SPL.FeaturePoint)spl.aFeaturePoint[i];
                        sumPrice += tempPoint.price;
                    }
                }
                return sumPrice;
            }

            // Optimization function 1  --- For SPL
            // !!! Need to be rewritten when apply to another problem;
            private double optFunc2(Genome genome)
            {
                int sumPerformance = 0;

                for (int i = 0; i < genome.GetLength(); i++)
                {
                    if (genome.aGene[i] == 1)
                    {
                        SPL.FeaturePoint tempPoint = (SPL.FeaturePoint)spl.aFeaturePoint[i];
                        sumPerformance += tempPoint.performance;
                    }
                }
                return sumPerformance;
            }
            #endregion

            // 判断一组配置是否合法
            public bool IsLegal(Genome genome)
            {
                //Firstly, we think about whether it's a legal tree-like model;
                for (int i = 0; i < genome.aGene.Length; i++)
                {
                    SPL.FeaturePoint t = ((SPL.FeaturePoint)spl.aFeaturePoint[i]);
                    if (genome.aGene[i] == 1)
                    {
                        t.isChosen = true;
                    }
                    else
                    {
                        t.isChosen = false;
                    }
                }

                //只有全员通过考验，才算通过
                foreach (SPL.FeaturePoint featurePoint in spl.aFeaturePoint)
                {
                    if (featurePoint.CheckValidity() == false)
                    {
                        return false; 
                    }
                }

                //Secondly, we consider about the constraints;
                foreach (SPL.Constraint constraint in spl.aConstraint)
                {
                    //如果A被选中,那么针对关系的不同,判断B在不在
                    if (genome.aGene[constraint.indexA] == 1)
                    {
                        switch (constraint.constraintRealtion)
                        {
                            case SPL.Constraint.relation.implied:
                                {
                                    if (genome.aGene[constraint.indexB] != 1)
                                    {
                                        return false;
                                    }
                                    break;
                                }
                            case SPL.Constraint.relation.excluded:
                                {
                                    if (genome.aGene[constraint.indexB] == 1)
                                    {
                                        return false;
                                    }
                                    break;
                                }
                        }
                    }
                }

                return true;
            }

            // 将一组不合法的配置变成合法配置
            public void TransToLegal(ref Genome genome)
            {
                //Console.Write("Before：");
                //for (int i = 0; i < genome.aGene.Length; i++)
                //{
                //    Console.Write(genome.aGene[i]);
                //}
                //Console.WriteLine();

                for (int i = 0; i < genome.aGene.Length; i++)
                {
                    SPL.FeaturePoint t = ((SPL.FeaturePoint)spl.aFeaturePoint[i]);
                    if (genome.aGene[i] == 1)
                    {
                        t.isChosen = true;
                    }
                    else
                    {
                        t.isChosen = false;
                    }
                }

                //纠错策略为，从第一个特征点开始，让每个点都没有问题
                foreach (SPL.FeaturePoint featurePoint in spl.aFeaturePoint)
                {
                    int error = featurePoint.GetErrorNumber();
                    while (error != 0)
                    {
                        switch (error)
                        {
                            //序号1: 强制节点没有选择 -- 那就选
                            case 1:
                                featurePoint.isChosen = true;
                                break;
                            //序号2：自己选择，父节点却没有选 -- 那自己也别选了，不然父亲还会出错
                            case 2:
                                featurePoint.isChosen = false;
                                break;
                            //序号3: Alternative关系出错 -- 那就把孩子全部放弃，挑一个选
                            case 3:
                                foreach (SPL.FeaturePoint children in featurePoint.children)
                                {
                                    children.isChosen = false;
                                }
                                int rd = random.Next(0, featurePoint.children.Length);
                                featurePoint.children[rd].isChosen = true;
                                break;
                            //序号4：Or关系出错(也就是说一个都没选) -- 那就让孩子每个都有概率选中
                            case 4:
                                foreach (SPL.FeaturePoint children in featurePoint.children)
                                {
                                    if (random.NextDouble() < 0.5)
                                    {
                                        children.isChosen = true;
                                    }
                                }
                                break;
                        }
                        error = featurePoint.GetErrorNumber();
                    }
                }

                for (int i = 0; i < spl.aFeaturePoint.Count; i++)
                {
                    SPL.FeaturePoint t = (SPL.FeaturePoint)spl.aFeaturePoint[i];
                    if (t.isChosen == true)
                    {
                        genome.aGene[i] = 1;
                    }
                    else
                    {
                        genome.aGene[i] = 0;
                    }
                }

                //Console.Write("After：");
                //for (int i = 0; i < genome.aGene.Length; i++)
                //{
                //    Console.Write(genome.aGene[i]);
                //}
                //Console.WriteLine();
            }

            // 用枚举方法强行找到软件产品线的Pareto前沿
            public ArrayList FindExactParetoFront()
            {
                ArrayList exactParetoFront;
                exactParetoFront = new ArrayList();
                if ((int)Math.Pow(2, spl.aFeaturePoint.Count) <= 0)
                {
                    MessageBox.Show("QAQ 超出范围了,不能用这个方式计算了");
                    return null;
                }
                Genome[] allPossibleSet = new Genome[(int)Math.Pow(2,spl.aFeaturePoint.Count)];
                for (int i = 0; i < allPossibleSet.Length; i++)
                {
                    //将每个基因基于编号编码, 最终实现全部的0,1取值
                    allPossibleSet[i] = new Genome();
                    Code(ref allPossibleSet[i],i);

                    if(IsLegal(allPossibleSet[i]) == true)
                    {
                        //一个标志位,判断其是否被前辈支配
                        bool beDominated = false;

                        //如果新的配置, 合法并且没有被前辈支配,那么它就可以进入帕里托前沿,同时如果它支配了前辈,那么前辈也会被删除

                        for (int j = 0; j < exactParetoFront.Count; j++)
                        {
                            if (Dominate(allPossibleSet[i], (Genome)exactParetoFront[j]) == true)
                            {
                                exactParetoFront.RemoveAt(j);
                                j--;
                            }
                            else if (Dominate((Genome)exactParetoFront[j], allPossibleSet[i]) == true)
                            {
                                beDominated = true;
                                break;
                            }
                        }
                        if (beDominated == false)
                        {
                            exactParetoFront.Add(allPossibleSet[i]);
                        }
                    }
                }            
                aParetoFront = exactParetoFront;
                return exactParetoFront;
            }

            // 判断一个基因是否支配了另一个
            public bool Dominate(Genome A, Genome B)
            {
                if (optFunc1(A) <= optFunc1(B) && optFunc2(A) >= optFunc2(B))
                {
                    return true;
                }
                return false;
            }

            // TEST EVENT
            public void test()
            {
                //Genome a = new Genome();
                //a.aGene[0] = 1;
                //a.aGene[1] = 1;
                //a.aGene[4] = 1;
                //MessageBox.Show(IsLegal(a).ToString());
                int c = 0;
                int total_price;
                int total_performance;
                Genome[] allPossibleSet = new Genome[(int)Math.Pow(2, spl.aFeaturePoint.Count)];
                for (int i = 0; i < allPossibleSet.Length; i++)
                {
                    //将每个基因基于编号编码, 最终实现全部的0,1取值
                    allPossibleSet[i] = new Genome();
                    Code(ref allPossibleSet[i], i);
                    if (IsLegal(allPossibleSet[i]) == true)
                    {
                        total_price = 0;
                        total_performance = 0;
                        c++;
                        for (int j = 0; j < allPossibleSet[i].aGene.Length;j++ )
                        {
                            Console.Write(allPossibleSet[i].aGene[j]);
                            if (allPossibleSet[i].aGene[j] == 1)
                            {
                                SPL.FeaturePoint t = (SPL.FeaturePoint)spl.aFeaturePoint[j];
                                total_price += t.price;
                                total_performance += t.performance;
                            }
                        }
                        Console.Write("total_price:" + total_price + "  total_performance" + total_performance);
                        Console.WriteLine();
                    }
                }
                MessageBox.Show(c.ToString());
            }
        }

        //---CLASS---   Indicator-based evolutionary algorithm engine
        class IBEAEng
        {
            IBEA ibea;

            //Constructor Function
            public IBEAEng(IBEA ibea)
            {
                this.ibea = ibea;
            }

            public ArrayList GetParetoFront()
            {
                while (ibea.GetParetoFront() == null)
                {
                    ibea.Epoch();
                }
                return ibea.GetParetoFront();
            }

            public void GetNextGeneration()
            {
                ibea.Epoch();
            }

            public ArrayList GetP()
            {
                return ibea.GetP();
            }

            public ArrayList GetQ()
            {
                return ibea.GetQ();
            }
        }
       
        //---   Functions about displaying sets on the screen
        #region
        //---   Initialize the display screen
        private void InitializeScreen() 
        {
            //Get initial parameters of the coordinates
            f1RangeFrom = double.Parse(f1From.Text) - double.Parse(f1Interval.Text) / 2;
            f1RangeTo = double.Parse(f1From.Text) + double.Parse(f1Interval.Text) * 5.5;

            f2RangeFrom = double.Parse(f1From.Text) - double.Parse(f1Interval.Text) / 2;
            f2RangeTo = double.Parse(f2From.Text) + double.Parse(f2Interval.Text) * 5.5;

            x1.Content = f1From.Text;
            x2.Content = (int.Parse(f1From.Text) + int.Parse(f1Interval.Text)).ToString();
            x3.Content = (int.Parse(f1From.Text) + int.Parse(f1Interval.Text) * 2).ToString();
            x4.Content = (int.Parse(f1From.Text) + int.Parse(f1Interval.Text) * 3).ToString();
            x5.Content = (int.Parse(f1From.Text) + int.Parse(f1Interval.Text) * 4).ToString();
            x6.Content = (int.Parse(f1From.Text) + int.Parse(f1Interval.Text) * 5).ToString();

            y1.Content = f2From.Text;
            y2.Content = (int.Parse(f2From.Text) + int.Parse(f2Interval.Text)).ToString();
            y3.Content = (int.Parse(f2From.Text) + int.Parse(f2Interval.Text) * 2).ToString();
            y4.Content = (int.Parse(f2From.Text) + int.Parse(f2Interval.Text) * 3).ToString();
            y5.Content = (int.Parse(f2From.Text) + int.Parse(f2Interval.Text) * 4).ToString();
            y6.Content = (int.Parse(f2From.Text) + int.Parse(f2Interval.Text) * 5).ToString();
            
        }

        //---   Set Coordinates according to  the input parameters
        private void SetCoordinates(object sender, RoutedEventArgs e)
        {
            f1RangeFrom = double.Parse(f1From.Text) - double.Parse(f1Interval.Text) / 2;
            f1RangeTo = double.Parse(f1From.Text) + double.Parse(f1Interval.Text) * 5.5;

            f2RangeFrom = double.Parse(f2From.Text) - double.Parse(f2Interval.Text) / 2;
            f2RangeTo = double.Parse(f2From.Text) + double.Parse(f2Interval.Text) * 5.5;

            x1.Content = f1From.Text;
            x2.Content = (int.Parse(f1From.Text) + int.Parse(f1Interval.Text)).ToString();
            x3.Content = (int.Parse(f1From.Text) + int.Parse(f1Interval.Text)*2).ToString();
            x4.Content = (int.Parse(f1From.Text) + int.Parse(f1Interval.Text)*3).ToString();
            x5.Content = (int.Parse(f1From.Text) + int.Parse(f1Interval.Text)*4).ToString();
            x6.Content = (int.Parse(f1From.Text) + int.Parse(f1Interval.Text)*5).ToString();

            y1.Content = f2From.Text;
            y2.Content = (int.Parse(f2From.Text) + int.Parse(f2Interval.Text)).ToString();
            y3.Content = (int.Parse(f2From.Text) + int.Parse(f2Interval.Text)*2).ToString();
            y4.Content = (int.Parse(f2From.Text) + int.Parse(f2Interval.Text)*3).ToString();
            y5.Content = (int.Parse(f2From.Text) + int.Parse(f2Interval.Text)*4).ToString();
            y6.Content = (int.Parse(f2From.Text) + int.Parse(f2Interval.Text)*5).ToString();

            ShowParetoFront();
        }

        //---   Convert (x,y) to the coordinates of Canvas
        private Point ConvertToCanvas(double x,double y)
        {
            Point canvasCoordinate = new Point();

            canvasCoordinate.X = (x - f1RangeFrom) / (f1RangeTo - f1RangeFrom) * coordinates.ActualWidth;
            canvasCoordinate.Y = (y - f2RangeFrom) / (f2RangeTo - f2RangeFrom) * coordinates.ActualHeight;

            return canvasCoordinate;
        }

        //---   Show the points of Pareto-Front on the display screen
        private void ShowParetoFront()
        {
            coordinates.Children.Clear();
            foreach (ParetoPoint p in array_ParetoFront) 
            {
                Point c = ConvertToCanvas(p.f1, p.f2);

                Ellipse dot = new Ellipse();
                dot.Width = 8;
                dot.Height = 8;
                dot.Stroke = null;
                dot.Fill = new SolidColorBrush(Colors.Red);
                dot.Opacity = 0.5;
                coordinates.Children.Add(dot);
                Canvas.SetLeft(dot, c.X - dot.Width / 2);
                Canvas.SetBottom(dot, c.Y - dot.Height / 2);
            }
        }

        //---   Show the points of Pareto-Front on the display screen
        private void ShowArrayList(ArrayList arraylist,Color color)
        {
           // coordinates.Children.Clear();
            foreach (ParetoPoint p in arraylist)
            {
                Point c = ConvertToCanvas(p.f1, p.f2);

                Ellipse dot = new Ellipse();
                dot.Width = 8;
                dot.Height = 8;
                dot.Stroke = null;
                dot.Fill = new SolidColorBrush(color);
                dot.Opacity = 0.5;
                coordinates.Children.Add(dot);
                Canvas.SetLeft(dot, c.X - dot.Width / 2);
                Canvas.SetBottom(dot, c.Y - dot.Height / 2);
            }
        }

        //---   Receive values from Generator subindow
        private void ReceiveValues(object sender, PassValuesEventArgs e)
        {
            this.spl = e.spl;
            SetArgsWithSPL();
        }

        //---   Set parameters according to the generated feature model
        private void SetArgsWithSPL()
        {
            //根据SPL设置基本参数
         //   MessageBox.Show("特征模型生成成功,参数已自动设置");
            rightPointBox.Text = Math.Pow(2, spl.aFeaturePoint.Count).ToString();
            rightPointBox.IsEnabled = false;
            leftPointBox.Text = "0";
            leftPointBox.IsEnabled = false;
        }

        //---   Event of test button
        private void TestEvent(object sender, RoutedEventArgs e)
        {
            double crossoverRate = Double.Parse(crossoverRateBox.Text);
            double mutationRate = Double.Parse(mutationRateBox.Text);
            double leftPoint = Double.Parse(leftPointBox.Text);
            double rightPoint = Double.Parse(rightPointBox.Text);
            int PSize = int.Parse(PSizeBox.Text);
            int QSize = int.Parse(QSizeBox.Text);
            int maxGeneration = int.Parse(maxGenerationBox.Text);

            ibea = new IBEA();
            ibea.Init(mutationRate, crossoverRate, leftPoint, rightPoint, PSize, QSize, maxGeneration, spl, spl.aFeaturePoint.Count);

            ibea.test();
        }

        //---   使用枚举法找到精确Pareto前沿
        private void FindExactParetoFront(object sender, RoutedEventArgs e)
        {
            DateTime beginTime = DateTime.Now;
            if (ibea.FindExactParetoFront() != null)
            {
                array_ParetoFront = ibeaEng.GetParetoFront();
                ShowParetoFront();
            }
            DateTime finishTime = DateTime.Now;
            MessageBox.Show("共找到Pareto前沿上的点" + array_ParetoFront.Count.ToString() + "个\n共耗时" + (finishTime - beginTime).TotalSeconds + "秒");
        }

        //---   打开模型生成器子窗口
        private void OpenFetureModelGenerator(object sender, RoutedEventArgs e)
        {
            FeatureModel generator = new FeatureModel();

            //订阅事件
            generator.PassValuesEvent += new FeatureModel.PassValuesHandler(ReceiveValues);

            generator.ShowDialog();
        }

        //Set IBEA parameters and start IBEA
        private void StartIBEA(object sender, RoutedEventArgs e)
        {
            DateTime beginTime = DateTime.Now;
            array_ParetoFront = ibeaEng.GetParetoFront();
            ShowParetoFront();
            DateTime finishTime = DateTime.Now;

            //计算所有解的重复数量
            int differentSolutions = 0;
            for (int i = 0; i < array_ParetoFront.Count - 1; i++)
            {
                int sameCount = 0;
                for (int j = i + 1; j < array_ParetoFront.Count; j++)
                {
                    ParetoPoint p1 = (ParetoPoint)array_ParetoFront[i];
                    ParetoPoint p2 = (ParetoPoint)array_ParetoFront[j];
                    if (p1.X == p2.X)
                    {
                        sameCount++;
                    }
                }
                if (sameCount == 0)
                {
                    differentSolutions++;
                }
            }
            MessageBox.Show("共找到Pareto前沿上的点" + array_ParetoFront.Count.ToString() + "个\n排除重复后有" + differentSolutions + "个不同的解\n共耗时" + (finishTime - beginTime).TotalSeconds + "秒");
        }

        //Set IBEA parameters
        private void SetIBEA(object sender, RoutedEventArgs e)
        {
            double crossoverRate = Double.Parse(crossoverRateBox.Text);
            double mutationRate = Double.Parse(mutationRateBox.Text);
            double leftPoint = Double.Parse(leftPointBox.Text);
            double rightPoint = Double.Parse(rightPointBox.Text);
            int PSize = int.Parse(PSizeBox.Text);
            int QSize = int.Parse(QSizeBox.Text);
            int maxGeneration = int.Parse(maxGenerationBox.Text);

            ibea = new IBEA();
            if (spl == null)
            {
                MessageBox.Show("请先生成特征模型");
                return;
            }
            ibea.Init(mutationRate, crossoverRate, leftPoint, rightPoint, PSize, QSize, maxGeneration,spl,spl.aFeaturePoint.Count);

            ibeaEng = new IBEAEng(ibea);

            array_ParetoFront = ibeaEng.GetP();
            ShowParetoFront();

            startButton.IsEnabled = true;
            nextButton.IsEnabled = true;
            exactButton.IsEnabled = true;
        }

        //Get next generation
        private void NextGeneration(object sender, RoutedEventArgs e)
        {
            coordinates.Children.Clear();
            for (int i = 0; i < 1; i++)
            {
               array_popP = ibeaEng.GetP();
               array_popQ = ibeaEng.GetQ();
               ibeaEng.GetNextGeneration();
            }
            ShowArrayList(array_popQ, Colors.Blue);
            ShowArrayList(array_popP, Colors.Red);
        }

        private void mutationRateBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }//End of MainWindow
}//End of namespace
#endregion