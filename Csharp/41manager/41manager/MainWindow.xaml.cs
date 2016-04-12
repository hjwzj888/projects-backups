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
using System.Data.SqlClient;
using System.Data;
using System.IO;
using Microsoft.Win32;

namespace _41manager
{
    /// <summary>
    /// 尚未解決的問題:
    /// 1. 由於背景模塊全部使用的是canvas,因此顯得很不靈活.比如必須手動設置clockCarrier和clockImage尺寸為120*120
    /// 2. 前台與邏輯耦合度很高, 如果要做移植會很困難
    /// 3. 比如VR裡面自帶一個類"對話框", 但它綁定所需要的元素(如Grid和TextBlock)卻在VR中不得不再次申明,似乎有些冗餘
    /// 4. VR反應中, 有一個生成隨機反應的隨機數生成器 它的參數是于行為數量一致的, 因此這裡耦合度有點點高, 可以考虑以后为用对象集合来解决
    /// 5. 记事本每次从数据库中读取整张表, 但却一条一条的单向操作 效率有待提高
    /// 6. 後台與前台的綁定應該只有carrier, 其他的用函數綁定就好. 但因為對其他元素的操作又要在構造函數中實現, 備選方案為設置Initialize函數, 有空可以改進下
    /// 7. 鈴聲的部分, 試聽的時候函數體里只有一句話, 雖然感覺層次比較分明, 但仍然有優化的餘地
    /// 8. 鬧鐘的輸入框可以改進, 雖然限制輸入的話, 用戶并不會出現使用錯誤, 但諸如12:1的用法還是會有人不習慣
    /// 9. 鬧鐘列表好麻煩..  就用簡單的添加刪除好了
    /// 10.Carrier的ZINDEX邏輯非常複雜..  暫時為了方便, 將bignote移出note的carrier. 負面影響在於它將不受底下小圖標的控制.
    /// 11.鬧鐘激活后的反應, 有待加入如推遲, 定時重鬧等功能
    /// </summary>
    public partial class MainWindow : Window
    {
        VisualEffectAnimation visualEffectAnimation;
        VirtualPersonality virtualPeronality;
        Clock clock;
        Alarm alarm;
        Note miniNote;
        Note bigNote;

        OpenFileDialog ofd = new OpenFileDialog();

        bool isMusicPlay = false;

        //---   程序主入口
        public MainWindow(){
            InitializeComponent();

            visualEffectAnimation = new VisualEffectAnimation();

            virtualPeronality = new VirtualPersonality(ref virtualPersonalityCarrier,ref textBubbleCarrier,ref word);
            virtualPeronality.BindBackground(ref virtualImage);
            virtualPeronality.SetSpeakFrequency(100);

            clock = new Clock(ref clockCarrier);
            clock.BindBackground(ref clockImage);

            alarm = new Alarm(ref alarmCarrier,ref alarmListCarrier,ref musicName);
            alarm.BindBackground(ref alarmImage);
            alarm.BindActivatedCarrier(ref alarmActivatedCarrier);

            miniNote = new Note(ref noteCarrier, ref noteText);
            miniNote.BindBackground(ref noteImage);
            miniNote.SetDatabaseTable("mininote");
            miniNote.LoadData();

            bigNote = new Note(ref bignoteCarrier,ref bignoteText);
            bigNote.SetInvisible();
            bigNote.BindBackground(ref noteImage);
            bigNote.SetDatabaseTable("bignote");
            bigNote.LoadData();

            this.ShowInTaskbar = false;
        }

        /// <summary>
        /// 面板類.也就是比如虛擬人格或者其他小模塊的父類.包含顯示模塊等等功能
        /// </summary>
        class Panel{
            protected bool visiable = true;   //用來判斷面板的顯示狀態
            protected Canvas carrier;  //用來記錄與該對象綁定的前台UI元素
            protected Image background; //用來綁定背景圖片, 如果有的話
            protected VisualEffectAnimation animation;  //用來進行一些視覺動畫

            //---   構造函數
            public Panel() {
                animation = new VisualEffectAnimation();
            }

            //---   顯示/關閉面板
            public void SwitchVisiability()
            {
                if (visiable == true){
                    animation.FadeOut(carrier, 0.5);
                    visiable = false;
                    Canvas.SetZIndex(carrier, -1);
                }
                else {
                    animation.FadeIn(carrier, 0.5);
                    visiable = true;
                    Canvas.SetZIndex(carrier, 1);
                }
            }

            //---   跳过动画部分, 直接设置其为不可见
            public void SetInvisible() {
                visiable = false;
                carrier.Opacity = 0;
                Canvas.SetZIndex(carrier,-1);
            }

            //---   綁定背景圖片
            public void BindBackground(ref Image background) {
                this.background = background;
            }

            //---   綁定carrier, 主要在panel單獨使用時使用
            public void BindCarrier(ref Canvas carrier) {
                this.carrier = carrier;
            }

            //---   返回carrier
            public Canvas GetCarrier(){
                return carrier;
            }

            //---   設置背景圖片源
            public void SetBackground(String sourcePath) {
                background.Source = new BitmapImage(new Uri(@"Image\" + sourcePath, UriKind.Relative));
            }
        }

        /// <summary>
        /// 虛擬人格類.包括對話框成員類, 以及其他自身屬性比如皮膚名稱,豆等等.
        /// </summary>
        class VirtualPersonality:Panel{
            private TextBubble textBubble;
            private Grid textBubbleCarrier;//主要是為了將前台與成員類進行綁定.
            private TextBlock textBubbleWord;//同上
            private DispatcherTimer speakTimer;//每分鐘(一般情況下)進行判斷, 看是否符合條件. 如果符合, 則說話
            private int speakFrequency;
            private int reactionKind = 4;//私有變量,記錄總共有多少種反應.主要是為了只修改這一處 

            //---   構造函數
            public VirtualPersonality(ref Canvas carrier, ref Grid textBubbleCarrier, ref TextBlock textBubbleWord)
            {
                //將自身carrier綁定到前台模塊
                this.carrier = carrier;

                //綁定元素, 并生成對話框成員類
                this.textBubbleCarrier = textBubbleCarrier;
                this.textBubbleWord = textBubbleWord;
                textBubble = new TextBubble(ref textBubbleCarrier, ref textBubbleWord);

                //默認每分鐘有20%的概率說話
                speakFrequency = 20;

                //開啟計時器, 每分鐘概率說話
                speakTimer = new DispatcherTimer();
                speakTimer.Interval = TimeSpan.FromMinutes(1);
                speakTimer.Tick += speakTimer_Tick;
                speakTimer.Start();
            }

            //---   計時器函數, 每秒生成一個100以內的隨機數, 如果小於20則隨機說話
            void speakTimer_Tick(object sender, EventArgs e){
                //用來判斷是否進行說話
                Random randomPossibility = new Random();
                //用來判斷如果說話, 進行那條反應
                Random randomBehavior = new Random();

                if (randomPossibility.Next(100) <= speakFrequency){
                    React(randomBehavior.Next(reactionKind) + 1);
                }    
            }

            //---   設置VR說話頻率, 如80則代表每分鐘有80%的概率說話
            public void SetSpeakFrequency(int value){
                speakFrequency = value;
            }

            //---   VR說話, 分別有調用文字泡函數, 使用音效等等
            public void Speak(String word) {
                textBubble.Speak(word);
            }

            //---   VR行為集合, 由給定的數字給予不同的反應
            public void React(int reactionNumber){
                switch (reactionNumber) {
                    case 1:{
                        Speak("今天天氣不錯誒");
                        break;
                    };
                    case 2: {
                        Speak("其實我才沒有那麼在乎徐璀啦");
                        break;
                    }
                    case 3:{
                        int hour = DateTime.Now.Hour;
                        if (hour >= 6 && hour <= 12){
                            Speak("おはよう 早安!");
                        }
                        else if (hour >= 12 && hour <= 18){
                            Speak("こんにちは 中午好!");
                        }
                        else {
                            Speak("こんばんは 晚上好!");
                        }
                        break;
                    }
                    case 4:
                        {
                            Speak("可以在我身上點右鍵看看喲");
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// 對話框類, 是VR的成員, 本身和Panel的定義不是非常相符, 但因為要借用其中的一些功能所以仍然繼承于Panel
        /// </summary>
        class TextBubble : Panel { 
            private Grid carrier;   //因為使用Grid而不是父類的Canvas作為載體, 因此再次申明了一下
            private TextBlock word; //與前台文字塊綁定
            private DispatcherTimer dispatcherTimer;//用來自動控制文字框消失

            //---   構造函數
            public TextBubble(ref Grid carrier, ref TextBlock word)
            {
                //由於對話泡泡默認不可見,因此修改visiable
                visiable = false;

                //將自身carrier綁定到前台模塊
                this.carrier = carrier;
                //將自身carrier綁定到前台模塊
                this.word = word;

                dispatcherTimer = new DispatcherTimer();
                dispatcherTimer.Interval = TimeSpan.FromSeconds(20);
                dispatcherTimer.Tick += dispatcherTimer_Tick;
            }

            //---   計時器,默認時間后漸隱對話框
            void dispatcherTimer_Tick(object sender, EventArgs e)
            {
                this.switchVisiability();
                dispatcherTimer.Stop();
            }

            //--- 修改文字,一段時間后漸隱
            public void Speak(String spokenWord) {
                word.Text = spokenWord;
                this.switchVisiability();
                dispatcherTimer.Start();
            }

            //---   顯示/關閉面板.因為carrier是Grid而不是默認的Canvas,因此進行了複寫.
            public void switchVisiability()
            {
                if (visiable == true)
                {
                    animation.FadeOut(carrier, 0.5);
                    visiable = false;
                }
                else
                {
                    animation.FadeIn(carrier, 0.5);
                    visiable = true;
                }
            }

        }

        /// <summary>
        /// 時鐘類, 主要包含時鐘指針的描繪, 時間校準算法等等
        /// </summary>
        class Clock : Panel {
            private Rectangle minPointer;   //分针
            private Rectangle hourPointer;  //时针
            private DispatcherTimer clockTimer; //计时器,每秒核實時間
            private RotateTransform rtfmin; //分针旋转动画
            private RotateTransform rtfhour;    //时针旋转动画

            //---   構造函數, 將時針分針等的基本位置確定
            public Clock(ref Canvas carrier){
                //將自身carrier綁定到前台模塊
                this.carrier = carrier;

                minPointer = new Rectangle();
                hourPointer = new Rectangle();
                rtfmin = new RotateTransform();
                rtfhour = new RotateTransform();

                //默認樣式為細白
                SetPointerWhite();

                carrier.Children.Add(minPointer);
                carrier.Children.Add(hourPointer);

                Canvas.SetLeft(minPointer, (carrier.Width - minPointer.Width) / 2);
                Canvas.SetTop(minPointer, carrier.Height / 2 - minPointer.Height);
                Canvas.SetLeft(hourPointer, (carrier.Width - hourPointer.Width) / 2);
                Canvas.SetTop(hourPointer, carrier.Height / 2 - hourPointer.Height);

                //設置指針旋轉中心 0.5表示為寬度的一半 1表示最低點
                Point renderTransformOrigin = new Point(0.5, 1);
                hourPointer.RenderTransformOrigin = renderTransformOrigin;
                minPointer.RenderTransformOrigin = renderTransformOrigin;

                //設置計時器 每秒觸發事件校準時間.
                alignTime();
                clockTimer = new DispatcherTimer();
                clockTimer.Interval = TimeSpan.FromSeconds(1);
                clockTimer.Tick += new EventHandler(clockTimer_Tick);
                clockTimer.Start();

 
            }

            //---  校準當前時間 
            private void alignTime()
            {
                double minute = double.Parse(string.Concat(DateTime.Now.ToString("mm")));
                double hour = double.Parse(string.Concat(DateTime.Now.ToString("hh")));
                rtfhour.Angle = (hour + minute / 60) * 30;
                hourPointer.RenderTransform = rtfhour;
                rtfmin.Angle = minute * 6;
                minPointer.RenderTransform = rtfmin;
            }

            //---   設置指針樣式:細白
            public void SetPointerWhite(){
                minPointer.Width = 3;
                minPointer.Height = 50;
                hourPointer.Width = 5;
                hourPointer.Height = 40;

                minPointer.Fill = new SolidColorBrush(Colors.White);
                minPointer.Stroke = new SolidColorBrush(Colors.Transparent);
                hourPointer.Fill = new SolidColorBrush(Colors.White);
                hourPointer.Stroke = new SolidColorBrush(Colors.Transparent);
            }

            //---   設置指針樣式:細黑
            public void SetPointerBlack()
            {
                minPointer.Width = 3;
                minPointer.Height = 50;
                hourPointer.Width = 5;
                hourPointer.Height = 40;

                minPointer.Fill = new SolidColorBrush(Colors.Black);
                minPointer.Stroke = new SolidColorBrush(Colors.Transparent);
                hourPointer.Fill = new SolidColorBrush(Colors.Black);
                hourPointer.Stroke = new SolidColorBrush(Colors.Transparent);
            }

            //---   計時器, 每秒更新. 校準時間
            private void clockTimer_Tick(object sender, EventArgs e){
                alignTime();
            }
        }

        /// <summary>
        /// 鬧鐘類, 包涵諸如鈴聲, 鬧鐘列表等信息
        /// </summary>
        class Alarm : Panel {
            private RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\41Manager");//用來記錄鈴聲的註冊表
            private MediaPlayer music;//鈴聲
            private TextBlock musicName;
            private AlarmList[] alarmLists;
            private int count;//表明目前有幾個鬧鐘
            private Canvas alarmActivatedCarrier;//激活鬧鐘后出現的對話框
            private Panel alarmListPanel;
            private DispatcherTimer dispatcherTimer;

            //---   構造函數
            public Alarm(ref Canvas carrier, ref Canvas alarmListCarrier ,ref TextBlock musicName)
            {
                //將自身carrier綁定到前台模塊
                this.carrier = carrier;

                //將列表carrier綁定到前台模塊
                alarmListPanel = new Panel();
                alarmListPanel.BindCarrier(ref alarmListCarrier);
                alarmListPanel.SetInvisible();

                //---   綁定鬧鈴名稱對話框
                this.musicName = musicName;

                music = new MediaPlayer();

                count = 0;

                alarmLists = new AlarmList[10];


                if (key == null)
                {
                    musicName.Text = "Please select music ";
                }
                else
                {
                    string musicname = (string)key.GetValue("SelMusic");
                    musicName.Text = musicname.Substring(musicname.LastIndexOf(@"\") + 1);
                    music.Open(new Uri((string)key.GetValue("SelMusic"), UriKind.Relative));
                }

                //創建計時器, 每秒檢查所有鬧鐘列表, 看是否激活
                dispatcherTimer = new DispatcherTimer();
                dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
                dispatcherTimer.Tick += dispatcherTimer_Tick;
                dispatcherTimer.Start();
            }

            //每秒檢查所有鬧鐘列表, 看是否激活, 若激活則播放鈴聲
            void dispatcherTimer_Tick(object sender, EventArgs e){
                for (int i = 0; i < count; i++){
                    if (alarmLists[i].IsActivated() == true){
                        PlayMusic();
                        alarmLists[i].Mute();
                        alarmActivatedCarrier.Opacity = 1;
                    }
                }
            }

            //---   綁定激活鬧鐘對話框
            public void BindActivatedCarrier(ref Canvas alarmActivatedCarrier){
                this.alarmActivatedCarrier = alarmActivatedCarrier;
            }

            //---   播放鈴聲
            public void PlayMusic(){
                if (music != null)
                    music.Play();
            }

            //---   停止鈴聲
            public void StopMusic() {
                music.Stop();
            }

            //---   設置鈴聲
            public void SetMusic(String musicPath) {
                music.Open(new Uri(musicPath, UriKind.Relative));
            }

            //---   添加鬧鐘
            public void AddAlarm(){
                if (count < alarmLists.Length){
                    alarmLists[count] = new AlarmList();
                    alarmListPanel.GetCarrier().Children.Add(alarmLists[count].GetCarrier());
                    Canvas.SetTop(alarmLists[count].GetCarrier(), 80 * count + 50);
                    Canvas.SetLeft(alarmLists[count].GetCarrier(), -50);
                    count++;
                }
                else{
                    MessageBox.Show("已經達到鬧鐘上限了喲");
                }
            }

            //---   刪除鬧鐘
            public void DeleteAlarm(){
                if (count > 0){
                    count--;
                    alarmListPanel.GetCarrier().Children.Remove(alarmLists[count].GetCarrier());
                }
            }

            //---   顯示鬧鐘列表
            public void ShowAlarmLists(){
                alarmListPanel.SwitchVisiability();
            }

            //---   从数据库中读取数据
            public void LoadData()
            {
                try
                {
                    string strcon, sql;
                    strcon = "Server=MALZAHAR\\MIBUSQL;Database=mibu;Trusted_Connection=SSPI";
                    SqlConnection conn = new SqlConnection(strcon);
                    conn.Open();

                    //先讀入count
                    sql = "select * from config";
                    SqlDataAdapter sda = new SqlDataAdapter(sql, conn);
                    DataTable dt = new DataTable();
                    sda.Fill(dt);

                    //按照讀取的鬧鐘數量, 添加鬧鐘響
                    for (int i = 0; i < (int)dt.Rows[0]["alarmCount"];i++ ){
                        AddAlarm();
                    }

                    sql = "select * from alarmLists";
                    sda = new SqlDataAdapter(sql, conn);
                    dt = new DataTable();
                    sda.Fill(dt);
                    for (int i = 0; i < count; i++)
                    {
                        //創建三個臨時變量, 用來存放讀出來的數據
                        int hour;
                        int minute;
                        int isActive;
                        hour = (int)dt.Rows[i]["hour"];
                        minute = (int)dt.Rows[i]["min"];
                        isActive = (int)dt.Rows[i]["Active"];
                        alarmLists[i].Initialize(hour,minute,isActive);
                    }
                    conn.Close();
                }
                catch (IOException ex)
                {
                    MessageBox.Show("读取數據失敗了!");
                    Console.WriteLine(ex.ToString());
                    Console.ReadLine();
                    return;
                }
            }

            //---   将数据存入数据库中
            public void SaveData()
            {
                try
                {
                    String strcon = "Server=MALZAHAR\\MIBUSQL;Database=mibu;Trusted_Connection=SSPI";
                    SqlConnection conn = new SqlConnection(strcon);
                    conn.Open();
                    String sql = "update config set alarmCount = " + count + " where [user] = 'default' ";
                    SqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    if (cmd.ExecuteNonQuery() != 1)
                        MessageBox.Show("虽然不知道是為什麼,但似乎保存鬧鐘數量時出错了 TwT");
                    for (int i = 0; i < count; i++)
                    {
                        sql = "update alarmlists set hour = " + alarmLists[i].GetHour() + ",min = " + alarmLists[i].GetMinute() + ",active = " + alarmLists[i].GetActive() + " where id = " + (i+1).ToString();
                        cmd.CommandText = sql;
                        if (cmd.ExecuteNonQuery() != 1)
                            MessageBox.Show("虽然不知道是為什麼,但似乎保存鬧鐘信息時出错了 TwT");
                    }
                    conn.Close();
                }
                catch (IOException ex)
                {
                    MessageBox.Show("保存數據失敗了!");
                    Console.WriteLine(ex.ToString());
                    Console.ReadLine();
                    return;
                }
            }
        }

        /// <summary>
        /// 鬧鐘列表類, 包含鬧鐘的各種信息, 比如時間等等
        /// </summary>
        class AlarmList {
            private Canvas carrier;
            ComboBox hourBox;
            ComboBox minBox;
            CheckBox isActiveBox;

            //---   構造函數 用代碼繪製一個基本的模型
            public AlarmList() {
                //設置基本佈局
                carrier = new Canvas();
                carrier.Width = 280;
                carrier.Height = 80;

                //設置背景
                ImageBrush backgroundBrush = new ImageBrush();
                backgroundBrush.ImageSource = new BitmapImage(new Uri(@"Image\Alarm_bgImage.png", UriKind.Relative));
                carrier.Background = backgroundBrush;

                //添加基本屬性控件
                //小時
                Label hourLabel = new Label();
                hourLabel.Content = "時";
                hourLabel.Foreground = new SolidColorBrush(Colors.White);
                carrier.Children.Add(hourLabel);
                Canvas.SetLeft(hourLabel,40);
                Canvas.SetTop(hourLabel,24);

                hourBox = new ComboBox();
                for (int i = 0; i <= 24; i++)
                {
                    hourBox.Items.Add(i);
                }
                hourBox.SelectedItem = 0;
                carrier.Children.Add(hourBox);
                Canvas.SetLeft(hourBox, 65);
                Canvas.SetTop(hourBox, 26);

                //分鐘
                Label minLabel = new Label();
                minLabel.Content = "分";
                minLabel.Foreground = new SolidColorBrush(Colors.White);
                carrier.Children.Add(minLabel);
                Canvas.SetLeft(minLabel, 120);
                Canvas.SetTop(minLabel, 24);

                minBox = new ComboBox();
                for (int i = 0; i <= 60; i++){
                    minBox.Items.Add(i);
                }
                minBox.SelectedItem = 0;
                carrier.Children.Add(minBox);
                Canvas.SetLeft(minBox, 143);
                Canvas.SetTop(minBox, 26);

                //是否激活
                isActiveBox = new CheckBox();
                isActiveBox.IsChecked = true;
                isActiveBox.Checked += isActive_Checked;
                isActiveBox.Unchecked += isActive_Unchecked;
                isActiveBox.Content = "啟動";
                isActiveBox.Foreground = new SolidColorBrush(Colors.White);
                carrier.Children.Add(isActiveBox);
                Canvas.SetLeft(isActiveBox,198);
                Canvas.SetTop(isActiveBox,29);
            }

            //---   用給定的參數,初始化鬧鐘項
            public void Initialize(int hour, int minute, int isActive){
                hourBox.SelectedItem = hour;
                minBox.SelectedItem = minute;
                if (isActive == 1)
                {
                    isActiveBox.IsChecked = true;
                }
                else
                    isActiveBox.IsChecked = false;

            }

            //---   取消選中checkbox時
            void isActive_Unchecked(object sender, RoutedEventArgs e){
                carrier.Opacity = 0.5;
                hourBox.IsEnabled = false;
                minBox.IsEnabled = false;
            }

            //---   選中checkbox時
            void isActive_Checked(object sender, RoutedEventArgs e){
                carrier.Opacity = 1;
                hourBox.IsEnabled = true;
                minBox.IsEnabled = true;
            }

            //---   返回carrier
            public Canvas GetCarrier() {
                return carrier;
            }

            //---   返回小時
            public int GetHour(){
                return (int)hourBox.SelectedItem;
            }

            //---   返回分鐘
            public int GetMinute(){
                return (int)minBox.SelectedItem;
            }

            //---   返回激活狀態
            public int GetActive(){
                if (isActiveBox.IsChecked == true)
                    return 1;
                else
                    return 0;
            }

            //---   判斷是否激活
            public bool IsActivated() {
                if (hourBox.SelectedItem != null && minBox.SelectedItem != null){
                    if (hourBox.SelectedItem.ToString() == DateTime.Now.Hour.ToString() && minBox.SelectedItem.ToString() == DateTime.Now.Minute.ToString() && isActiveBox.IsChecked == true)
                        return true;
                    else
                        return false;
                }
                return false;
            }

            //---   將鬧鐘靜音
            public void Mute(){
                isActiveBox.IsChecked = false;
            }
        }

        /// <summary>
        /// 記事本類, 最主要的功能在於存儲與讀取數據
        /// </summary>
        class Note : Panel {
            private TextBox text;
            private String table;
            private SqlDataAdapter sda;
            private DataTable dt;
            private int page;

            public Note(ref Canvas carrier, ref TextBox text) {
                //將自身carrier綁定到前台模塊
                this.carrier = carrier;

                //將自身textBox綁定到前天模塊
                this.text = text;

                page = 1;
            }

            //---   设置与之關聯的數據庫表的名稱
            public void SetDatabaseTable(String tableName) {
                table = tableName;
            }

            //---   从数据库中读取数据
            public void LoadData() {
                try {
                    string strcon, sql;
                    strcon = "Server=MALZAHAR\\MIBUSQL;Database=mibu;Trusted_Connection=SSPI";
                    sql = "select * from "+ table;
                    SqlConnection conn = new SqlConnection(strcon);
                    conn.Open();
                    sda = new SqlDataAdapter(sql, conn);
                    DataTable dt = new DataTable();
                    sda.Fill(dt);
                    //如果当前页数超出总行数(即翻至新的一页),则插入一条
                    if (dt.Rows.Count < page) {
                        SqlCommand cmd = conn.CreateCommand();
                        cmd.CommandText = "insert into "+ table +"(Page,Content) values (" + page.ToString() + ",'')";
                        if (cmd.ExecuteNonQuery() != 1)
                            MessageBox.Show("虽然不知道是哪里,但似乎是出错了 TwT");

                        sda = new SqlDataAdapter(sql, conn);
                        dt = new DataTable();
                        sda.Fill(dt);
                    }
                    text.Text = dt.Rows[page-1]["Content"].ToString();
                    conn.Close();
                }
                catch (IOException ex) {
                    MessageBox.Show("读取數據失敗了!");
                    Console.WriteLine(ex.ToString());
                    Console.ReadLine();
                    return;
                }
            }

            //---   将数据存入数据库中
            public void SaveData() {
                try {
                    String strcon = "Server=MALZAHAR\\MIBUSQL;Database=mibu;Trusted_Connection=SSPI";
                    String sql = "update " + table + " set Content = '" + text.Text + "' where Page = " + page.ToString();
                    SqlConnection conn = new SqlConnection(strcon);
                    conn.Open();
                    SqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    if(cmd.ExecuteNonQuery()!=1)
                        MessageBox.Show("虽然不知道是哪里,但似乎是出错了 TwT");
                }
                catch (IOException ex) {
                    MessageBox.Show("保存數據失敗了!");
                    Console.WriteLine(ex.ToString());
                    Console.ReadLine();
                    return;
                }
            }

            //---   下一页
            public void ToNextPage() {
                page++;
                LoadData();
            }

            //---   上一页
            public void ToPrevPage() {
                if (page > 1) {
                    page--;
                    LoadData();
                }
            }

        } 

        /// <summary>
        /// 視覺元素類, 包括一些淡入淡出動畫
        /// </summary>
        class VisualEffectAnimation
        {
            //---   漸入動畫,支持直線移動
            //---   參數1:待移動圖像 參數2:淡入時間(秒)  參數3:開始時間(秒)
            public void FadeIn(FrameworkElement movingObject, Double duration = 0.5, Double beginTime = 0)
            {
                DoubleAnimation fadeinAnimation = new DoubleAnimation();
                fadeinAnimation.Duration = TimeSpan.FromSeconds(duration);
                fadeinAnimation.From = movingObject.Opacity;
                fadeinAnimation.To = 1;
                fadeinAnimation.AccelerationRatio = 1;
                fadeinAnimation.BeginTime = TimeSpan.FromSeconds(beginTime);
                movingObject.BeginAnimation(FrameworkElement.OpacityProperty, fadeinAnimation);
            }

            public void FadeIn(Canvas movingObject, Double duration = 0.5, Double beginTime = 0)
            {
                DoubleAnimation fadeinAnimation = new DoubleAnimation();
                fadeinAnimation.Duration = TimeSpan.FromSeconds(duration);
                fadeinAnimation.From = movingObject.Opacity;
                fadeinAnimation.To = 1;
                fadeinAnimation.AccelerationRatio = 1;
                fadeinAnimation.BeginTime = TimeSpan.FromSeconds(beginTime);
                movingObject.BeginAnimation(Canvas.OpacityProperty, fadeinAnimation);
            }

            public void FadeIn(Grid movingObject, Double duration = 0.5, Double beginTime = 0)
            {
                DoubleAnimation fadeinAnimation = new DoubleAnimation();
                fadeinAnimation.Duration = TimeSpan.FromSeconds(duration);
                fadeinAnimation.From = movingObject.Opacity;
                fadeinAnimation.To = 1;
                fadeinAnimation.AccelerationRatio = 1;
                fadeinAnimation.BeginTime = TimeSpan.FromSeconds(beginTime);
                movingObject.BeginAnimation(Grid.OpacityProperty, fadeinAnimation);
            }

            //---   漸出動畫,支持直線移動
            //---   參數1:待移動圖像  參數2:淡出時間(秒)  參數3:開始時間(秒)
            public void FadeOut(FrameworkElement movingObject, Double duration = 0.5, Double beginTime = 0)
            {
                DoubleAnimation fadeOutAnimation = new DoubleAnimation();
                fadeOutAnimation.Duration = TimeSpan.FromSeconds(duration);
                fadeOutAnimation.From = movingObject.Opacity;
                fadeOutAnimation.To = 0;
                fadeOutAnimation.AccelerationRatio = 1;
                fadeOutAnimation.BeginTime = TimeSpan.FromSeconds(beginTime);
                movingObject.BeginAnimation(FrameworkElement.OpacityProperty, fadeOutAnimation);
            }

            public void FadeOut(Canvas movingObject, Double duration = 0.5, Double beginTime = 0)
            {
                DoubleAnimation fadeOutAnimation = new DoubleAnimation();
                fadeOutAnimation.Duration = TimeSpan.FromSeconds(duration);
                fadeOutAnimation.From = movingObject.Opacity;
                fadeOutAnimation.To = 0;
                fadeOutAnimation.AccelerationRatio = 1;
                fadeOutAnimation.BeginTime = TimeSpan.FromSeconds(beginTime);
                movingObject.BeginAnimation(Canvas.OpacityProperty, fadeOutAnimation);
            }

            public void FadeOut(Grid movingObject, Double duration = 0.5, Double beginTime = 0)
            {
                DoubleAnimation fadeOutAnimation = new DoubleAnimation();
                fadeOutAnimation.Duration = TimeSpan.FromSeconds(duration);
                fadeOutAnimation.From = movingObject.Opacity;
                fadeOutAnimation.To = 0;
                fadeOutAnimation.AccelerationRatio = 1;
                fadeOutAnimation.BeginTime = TimeSpan.FromSeconds(beginTime);
                movingObject.BeginAnimation(Grid.OpacityProperty, fadeOutAnimation);
            }
        }

        //---   雜項函數
        #region

        //---   測試功能鍵

        private void TEST_Click(object sender, RoutedEventArgs e)
        {
           // alarm.LoadData();
        }

        //---   打開窗口时, 讀取所有数据
        private void LoadData(object sender, RoutedEventArgs e)
        {
            alarm.LoadData();
        }

        //---   关闭窗口时, 保存所有数据
        private void SaveData(object sender, EventArgs e){
            miniNote.SaveData();
            bigNote.SaveData();
            alarm.SaveData();
        }

        //---   鼠标互动事件
        #region
        //---   拖動窗口
        private void DragMove(object sender, MouseButtonEventArgs e) {
            this.DragMove();
        }

        //---   小记事本翻到下一页,并保存当前记录
        private void miniNoteNextPage(object sender, MouseButtonEventArgs e){
            miniNote.SaveData();
            miniNote.ToNextPage();
        }

        //---   小记事本翻到上一页,并保存当前记录
        private void miniNotePrevPage(object sender, MouseButtonEventArgs e){
            miniNote.SaveData();
            miniNote.ToPrevPage();
        }

        //---   大记事本翻到下一页,并保存当前记录
        private void bigNoteNextPage(object sender, MouseButtonEventArgs e){
            bigNote.SaveData();
            bigNote.ToNextPage();
        }

        //---   大记事本翻到上一页,并保存当前记录
        private void bigNotePrevPage(object sender, MouseButtonEventArgs e){
            bigNote.SaveData();
            bigNote.ToPrevPage();
        }

        //---   显示/隐藏大记事本
        private void ShowBigNote(object sender, MouseButtonEventArgs e){
            bigNote.SwitchVisiability();
        }

        //---   显示/隐藏时钟
        private void ShowClock(object sender, MouseButtonEventArgs e){
            clock.SwitchVisiability();
        }

        //---   显示/隐藏闹钟
        private void ShowAlarm(object sender, MouseButtonEventArgs e){
            alarm.SwitchVisiability();
        }

        //---   显示/隐藏记事本
        private void ShowMiniNote(object sender, MouseButtonEventArgs e){
            miniNote.SwitchVisiability();
        }

        //---   漸入所有圖標
        private void FadeIn_Icons(object sender, MouseEventArgs e){
            visualEffectAnimation.FadeIn(clockIcon);
            visualEffectAnimation.FadeIn(alarmIcon,0.5,0.2);
            visualEffectAnimation.FadeIn(noteIcon,0.5,0.4);
            visualEffectAnimation.FadeIn(exitIcon,0.5,0.6);
        }

        //---   漸出所有圖標
        private void FadeOut_Icons(object sender, MouseEventArgs e){
            visualEffectAnimation.FadeOut(clockIcon);
            visualEffectAnimation.FadeOut(alarmIcon, 0.5, 0.2);
            visualEffectAnimation.FadeOut(noteIcon, 0.5, 0.4);
            visualEffectAnimation.FadeOut(exitIcon, 0.5, 0.6);
        }

        //---   關閉窗口
        private void EXIT(object sender, MouseButtonEventArgs e){
            App.Current.Shutdown();
        }      

        //---   渐入记事本箭头
        private void FadeIn_ShowBigNoteButton(object sender, MouseEventArgs e){
            visualEffectAnimation.FadeIn(showBigNoteButton);
        }

        //---   渐出记事本箭头
        private void FadeOut_ShowBigNoteButton(object sender, MouseEventArgs e){
            visualEffectAnimation.FadeOut(showBigNoteButton);
        }

        //---   試聽音樂
        private void PlayMusic(object sender, MouseButtonEventArgs e){
            if (isMusicPlay == true){
                alarm.StopMusic();
                isMusicPlay = false;
            }
            else{
                alarm.PlayMusic();
                isMusicPlay = true;
            }
        }

        //---   選擇音樂
        private void SelectMusic(object sender, MouseButtonEventArgs e){
            //过滤选择文件的格式                                                                                                                                                                     
            ofd.Filter = "(mp3文件)|*.mp3|(wma文件)|*.wma|(wav文件)|*.wav|(所有文件)|*.*|別開玩笑了魂淡|*.⑨";
            ofd.Title = "音楽を選択してください..";
            if (((bool)ofd.ShowDialog().GetValueOrDefault())){
                alarm.SetMusic(ofd.FileName);
                musicName.Text = ofd.FileName.Substring(ofd.FileName.LastIndexOf(@"\") + 1);
            }
            if (ofd.FileName.Length != 0){
                RegistryKey key1 = Registry.CurrentUser.CreateSubKey("SOFTWARE\\41Manager");
                key1.SetValue("SelMusic", ofd.FileName);
            }
        }

        //---   添加鬧鐘
        private void AddAlarm(object sender, RoutedEventArgs e){
            alarm.AddAlarm();
        }

        //---   刪除鬧鐘
        private void DeleteAlarm(object sender, RoutedEventArgs e){
            alarm.DeleteAlarm();
        }

        //---   靜音鬧鐘
        private void MuteAlarm(object sender, RoutedEventArgs e){
            alarmActivatedCarrier.Opacity = 0;
            alarm.StopMusic();
        }

        //---   顯示/關閉鬧鐘列表
        private void ShowAlarmLists(object sender, MouseButtonEventArgs e){
            alarm.ShowAlarmLists();
        }

        //---   開啟/關閉置頂功能
        private void SetTopmost(object sender, MouseButtonEventArgs e){
            if (this.Topmost == true){
                MessageBox.Show("已取消置頂功能, 米布布會在桌面上等你的 ︿(￣︶￣)︿");
                this.Topmost = false;
            }
            else{
                MessageBox.Show("開啟置頂功能, 又可以一直見面啦 ╰(*°▽°*)╯");
                this.Topmost = true;
            }
        }

 #endregion





    


    }//end of  public partial class MainWindow : Window
}//end of namespace _41manager
         #endregion