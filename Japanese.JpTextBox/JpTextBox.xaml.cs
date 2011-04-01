using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;
using System.Windows.Automation.Peers;
using System.Windows.Data;

namespace Japanese
{
    public partial class JpTextBox : UserControl, INotifyPropertyChanged
    {
        // 依存プロパティ
        public static readonly DependencyProperty IsImeEnabledProperty
            = DependencyProperty.Register("IsImeEnabled", typeof(bool), typeof(JpTextBox), null);
        public static readonly DependencyProperty AcceptsReturnProperty
            = DependencyProperty.Register("AcceptsReturn", typeof(bool), typeof(JpTextBox), null);
        public static readonly DependencyProperty CaretBrushProperty
            = DependencyProperty.Register("CaretBrush", typeof(Brush), typeof(JpTextBox), null);
        public static readonly DependencyProperty InputScopeProperty
            = DependencyProperty.Register("InputScope", typeof(InputScope), typeof(JpTextBox), null);
        public static readonly DependencyProperty IsReadOnlyProperty
            = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(JpTextBox), null);
        public static readonly DependencyProperty MaxLengthProperty
            = DependencyProperty.Register("MaxLength", typeof(int), typeof(JpTextBox), null);
        public static readonly DependencyProperty SelectionBackgroundProperty
            = DependencyProperty.Register("SelectionBackground", typeof(Brush), typeof(JpTextBox), null);
        public static readonly DependencyProperty SelectionForegroundProperty
            = DependencyProperty.Register("SelectionForeground", typeof(Brush), typeof(JpTextBox), null);
        public static readonly DependencyProperty TextAlignmentProperty
            = DependencyProperty.Register("TextAlignment", typeof(TextAlignment), typeof(JpTextBox), null);
        public static readonly DependencyProperty TextProperty
            = DependencyProperty.Register("Text", typeof(string), typeof(JpTextBox), null);
        public static readonly DependencyProperty TextWrappingProperty
            = DependencyProperty.Register("TextWrapping", typeof(TextWrapping), typeof(JpTextBox), null);

        private Brush TextBoxBorderBrush
        {
            get
            {
                if (IsImeEnabled) return Application.Current.Resources["PhoneAccentBrush"] as Brush;
                else return Application.Current.Resources["PhoneContrastBackgroundBrush"] as Brush;
            }
        }

        // プロパティ
        public bool IsImeEnabled { get { return (bool)this.GetValue(IsImeEnabledProperty); } set { this.SetValue(IsImeEnabledProperty, value); } }
        public bool AcceptsReturn { get { return textBox.AcceptsReturn; } set { textBox.AcceptsReturn = value; } }
        public Brush CaretBrush { get { return textBox.CaretBrush; } set { textBox.CaretBrush = value; } }
        public FontSource FontSource { get { return textBox.FontSource; } set { textBox.FontSource = value; } }
        public ScrollBarVisibility HorizontalScrollBarVisibility { get { return textBox.HorizontalScrollBarVisibility; } set { textBox.HorizontalScrollBarVisibility = value; } }
        public InputScope InputScope { get { return textBox.InputScope; } set { textBox.InputScope = value; } }
        public bool IsReadOnly { get { return textBox.IsReadOnly; } set { textBox.IsReadOnly = value; } }
        public int MaxLength { get { return textBox.MaxLength; } set { textBox.MaxLength = value; } }
        public string SelectedText { get { return textBox.SelectedText; } set { textBox.SelectedText = value; } }
        public Brush SelectionBackground { get { return textBox.SelectionBackground; } set { textBox.SelectionBackground = value; } }
        public Brush SelectionForeground { get { return textBox.SelectionForeground; } set { textBox.SelectionForeground = value; } }
        public int SelectionLength { get { return textBox.SelectionLength; } set { textBox.SelectionLength = value; } }
        public int SelectionStart { get { return textBox.SelectionStart; } set { textBox.SelectionStart = value; } }
        public string Text { get { return textBox.Text; } set { textBox.Text = value; } }
        public TextAlignment TextAlignment { get { return textBox.TextAlignment; } set { textBox.TextAlignment = value; } }
        public TextWrapping TextWrapping { get { return textBox.TextWrapping; } set { textBox.TextWrapping = value; } }
        public ScrollBarVisibility VerticalScrollBarVisibility { get { return textBox.VerticalScrollBarVisibility; } set { textBox.VerticalScrollBarVisibility = value; } }
        
        new public bool IsEnabled
        {
            get { return textBox.IsEnabled; }
            set
            {
                textBox.IsEnabled = value;
                if (value == true && compositionBox.Text != "")
                {
                    compositionBox.Visibility = System.Windows.Visibility.Visible;
                    compositionListPopup.IsOpen = true;
                }
                if (value == false)
                {
                    compositionBox.Visibility = System.Windows.Visibility.Collapsed;
                    compositionListPopup.IsOpen = false;
                }
            }
        }
        public int CompositionDelay { get { return 150; } }

        public event RoutedEventHandler SelectionChanged;
        public event TextChangedEventHandler TextChanged;
        public event EventHandler StartComposition;
        public event EventHandler EndComposition;

        public Rect GetRectFromCharacterIndex(int charIndex) { return textBox.GetRectFromCharacterIndex(charIndex); }
        public Rect GetRectFromCharacterIndex(int charIndex, bool trailingEdge) { return textBox.GetRectFromCharacterIndex(charIndex, trailingEdge); }
        //protected override AutomationPeer OnCreateAutomationPeer();
        //protected override void OnGotFocus(RoutedEventArgs e);
        //protected override void OnKeyDown(KeyEventArgs e);
        //protected override void OnKeyUp(KeyEventArgs e);
        //protected override void OnLostFocus(RoutedEventArgs e);
        //protected override void OnMouseEnter(MouseEventArgs e);
        //protected override void OnMouseLeave(MouseEventArgs e);
        //protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e);
        //protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e);
        //protected override void OnMouseMove(MouseEventArgs e);
        public void Select(int start, int length) { textBox.Select(start, length); }
        public void SelectAll() { textBox.SelectAll(); }

        /// <summary>
        /// ローマ字変換テーブル
        /// </summary>
        static readonly Dictionary<string, string> m_romajiToKanaTable = new Dictionary<string, string>()
        {
            {"0","０"},{"1","１"},{"2","２"},{"3","３"},{"4","４"},{"5","５"},{"6","６"},{"7","７"},{"8","８"},{"9","９"},
            {".","。"},{",","、"},{"-","ー"},{"~","?"},{"?","？"},{"!","！"},{"va","ヴぁ"},{"vi","ヴぃ"},{"vu","ヴ"},{"ve","ヴぇ"},{"vo","ヴぉ"},{"vv", "っ"},{"xx", "っ"},{"kk", "っ"},{"gg", "っ"},{"ss", "っ"},{"zz", "っ"},
            {"jj", "っ"},{"tt", "っ"},{"dd", "っ"},{ "hh", "っ"},{"ff", "っ"},{"bb","っ"},{"pp", "っ"},{"mm","っ"},{"yy", "っ"},{"rr","っ"},{"ww", "っ"},{"cc", "っ"},{"kya","きゃ"},{"kyi","きぃ"},
            {"kyu","きゅ"},{"kye","きぇ"},{"kyo","きょ"},{"gya","ぎゃ"},{"gyi","ぎぃ"},{"gyu","ぎゅ"},{"gye","ぎぇ"},{"gyo","ぎょ"},{"sya","しゃ"},{"syi","しぃ"},{"syu","しゅ"},{"sye","しぇ"},{"syo","しょ"},
            {"sha","しゃ"},{"shi","し"},{"shu","しゅ"},{"she","しぇ"},{"sho","しょ"},{"zya","じゃ"},{"zyi","じぃ"},{"zyu","じゅ"},{"zye","じぇ"},{"zyo","じょ"},{"tya","ちゃ"},{"tyi","ちぃ"},{"tyu","ちゅ"},
            {"tye","ちぇ"},{"tyo","ちょ"},{"cha","ちゃ"},{"chi","ち"},{"chu","ちゅ"},{"che","ちぇ"},{"cho","ちょ"},{"dya","ぢゃ"},{"dyi","ぢぃ"},{"dyu","ぢゅ"},{"dye","ぢぇ"},{"dyo","ぢょ"},{"tha","てゃ"},
            {"thi","てぃ"},{"thu","てゅ"},{"the","てぇ"},{"tho","てょ"},{"dha","でゃ"},{"dhi","でぃ"},{"dhu","でゅ"},{"dhe","でぇ"},{"dho","でょ"},{"nya","にゃ"},{"nyi","にぃ"},{"nyu","にゅ"},{"nye","にぇ"},
            {"nyo","にょ"},{"jya","じゃ"},{"jyi","じ"},{"jyu","じゅ"},{"jye","じぇ"},{"jyo","じょ"},{"hya","ひゃ"},{"hyi","ひぃ"},{"hyu","ひゅ"},{"hye","ひぇ"},{"hyo","ひょ"},{"bya","びゃ"},{"byi","びぃ"},
            {"byu","びゅ"},{"bye","びぇ"},{"byo","びょ"},{"pya","ぴゃ"},{"pyi","ぴぃ"},{"pyu","ぴゅ"},{"pye","ぴぇ"},{"pyo","ぴょ"},{"fa","ふぁ"},{"fi","ふぃ"},{"fe","ふぇ"},{"fo","ふぉ"},{"mya","みゃ"},
            {"myi","みぃ"},{"myu","みゅ"},{"mye","みぇ"},{"myo","みょ"},{"rya","りゃ"},{"ryi","りぃ"},{"ryu","りゅ"},{"rye","りぇ"},{"ryo","りょ"},{"n\"","ん"},{"nn","ん"},{"a","あ"},{"i","い"},{"u","う"},
            {"e","え"},{"o","お"},{"xa","ぁ"},{"xi","ぃ"},{"xu","ぅ"},{"xe","ぇ"},{"xo","ぉ"},{"la","ぁ"},{"li","ぃ"},{"lu","ぅ"},{"le","ぇ"},{"lo","ぉ"},{"ka","か"},{"ki","き"},{"ku","く"},{"ke","け"},{"ko","こ"},
            {"ga","が"},{"gi","ぎ"},{"gu","ぐ"},{"ge","げ"},{"go","ご"},{"sa","さ"},{"si","し"},{"su","す"},{"se","せ"},{"so","そ"},{"za","ざ"},{"zi","じ"},{"zu","ず"},{"ze","ぜ"},{"zo","ぞ"},{"ja","じゃ"},{"ji","じ"},
            {"ju","じゅ"},{"je","じぇ"},{"jo","じょ"},{"ta","た"},{"ti","ち"},{"tu","つ"},{"tsu","つ"},{"te","て"},{"to","と"},{"da","だ"},{"di","ぢ"},{"du","づ"},{"de","で"},{"do","ど"},{"xtu","っ"},{"xtsu","っ"},
            {"na","な"},{"ni","に"},{"nu","ぬ"},{"ne","ね"},{"no","の"},{"ha","は"},{"hi","ひ"},{"hu","ふ"},{"fu","ふ"},{"he","へ"},{"ho","ほ"},{"ba","ば"},{"bi","び"},{"bu","ぶ"},{"be","べ"},{"bo","ぼ"},{"pa","ぱ"},
            {"pi","ぴ"},{"pu","ぷ"},{"pe","ぺ"},{"po","ぽ"},{"ma","ま"},{"mi","み"},{"mu","む"},{"me","め"},{"mo","も"},{"xya","ゃ"},{"ya","や"},{"xyu","ゅ"},{"yu","ゆ"},{"xyo","ょ"},{"yo","よ"},{"ra","ら"},{"ri","り"},
            {"ru","る"},{"re","れ"},{"ro","ろ"},{"xwa","ゎ"},{"wa","わ"},{"wi","うぃ"},{"we","うぇ"},{"wo","を"}
        };

        WebClient m_client;
        Timer m_timer;
        List<string> m_requestQueue;

        public JpTextBox()
        {
            InitializeComponent();
            this.DataContext = this;

            // TextBoxへのバインディングを設定
            textBox.SetBinding(TextBox.AcceptsReturnProperty, new Binding("AcceptsReturn") { Source = this, Mode = BindingMode.TwoWay });
            textBox.SetBinding(TextBox.CaretBrushProperty, new Binding("CaretBrush") { Source = this, Mode = BindingMode.TwoWay });
            textBox.SetBinding(TextBox.InputScopeProperty, new Binding("InputScope") { Source = this, Mode = BindingMode.TwoWay });
            textBox.SetBinding(TextBox.IsReadOnlyProperty, new Binding("IsReadOnly") { Source = this, Mode = BindingMode.TwoWay });
            textBox.SetBinding(TextBox.MaxLengthProperty, new Binding("MaxLength") { Source = this, Mode = BindingMode.TwoWay });
            textBox.SetBinding(TextBox.SelectionBackgroundProperty, new Binding("SelectionBackground") { Source = this, Mode = BindingMode.TwoWay });
            textBox.SetBinding(TextBox.SelectionForegroundProperty, new Binding("SelectionForeground") { Source = this, Mode = BindingMode.TwoWay });
            textBox.SetBinding(TextBox.TextAlignmentProperty, new Binding("TextAlignment") { Source = this, Mode = BindingMode.TwoWay });
            textBox.SetBinding(TextBox.TextProperty, new Binding("Text") { Source = this, Mode = BindingMode.TwoWay });
            textBox.SetBinding(TextBox.TextWrappingProperty, new Binding("TextWrapping") { Source = this, Mode = BindingMode.TwoWay });

            // TextBoxの初期値を設定
            AcceptsReturn = false;
            //CaretBrush
            //InputScope
            IsReadOnly = false;
            MaxLength = 0;
            //SelectionBackground
            //SelectionForeground
            TextAlignment = System.Windows.TextAlignment.Left;
            Text = "";
            TextWrapping = System.Windows.TextWrapping.NoWrap;

            // WebClientを初期化
            m_requestQueue = new List<string>();
            m_timer = new Timer(new TimerCallback(m_timer_TimerCallback), null, Timeout.Infinite, Timeout.Infinite);
            m_client = new WebClient();
            m_client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(m_client_DownloadStringCompleted);
        }

        void m_timer_TimerCallback(object state)
        {
            RequestSocialIME();
        }

        void m_client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            m_requestQueue.Remove(e.UserState as string);
            try
            {
                compositionList.ItemsSource = e.Result.Trim().Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (compositionList.Items.Count > 0)
                    compositionList.ScrollIntoView(compositionList.Items[0]);
            } catch { }
            if (m_requestQueue.Count > 0)
            {
                m_client.DownloadStringAsync(new Uri(m_requestQueue[0]), m_requestQueue[0]);
            }
        }

        public void SetCompositionMode(bool flag)
        {
            IsImeEnabled = flag;
            if (IsImeEnabled)
            {
                textBox.BorderBrush = Application.Current.Resources["PhoneAccentBrush"] as Brush;
                textBox.Background = Application.Current.Resources["PhoneAccentBrush"] as Brush;
            }
            else
            {
                textBox.BorderBrush = Application.Current.Resources["PhoneBorderBrush"] as Brush;
                textBox.Background = Application.Current.Resources["PhoneTextBoxBrush"] as Brush;
                CloseConjecture();
            }
        }

        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.PlatformKeyCode == 12 || e.PlatformKeyCode == 227)
            {
                SetCompositionMode(!IsImeEnabled);
                e.Handled = true;
                return;
            }
            if (IsImeEnabled)
            {
                char chr = keyToChar(e.Key);
                if (chr != '\0')
                {
                    e.Handled = true;
                    OpenConjecture(chr.ToString());
                }
            }
        }

        private void compositionBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // キャレット位置までの文字列を得る
            string text_all = compositionBox.Text;
            string text1 = text_all.Substring(0, compositionBox.SelectionStart);

            // 末尾までのアルファベットを取得
            Match m = Regex.Match(text1, "[a-zA-Z_0-9,.!?-]+$");
            if (m.Success)
            {
                if (m_romajiToKanaTable.ContainsKey(m.Value))
                {
                    // 当該範囲置換
                    compositionBox.Text = text_all.Substring(0, m.Index)
                        + m_romajiToKanaTable[m.Value]
                        + text_all.Substring(text1.Length);

                    // キャレット位置設定
                    compositionBox.SelectionStart = m.Index + m_romajiToKanaTable[m.Value].Length;

                    // Socal IME へリクエスト
                    //RequestSocialIME();
                    m_timer.Change(CompositionDelay, Timeout.Infinite);
                }
                else if(m.Value.Length >= 2 && m.Value[0].ToString().ToLower() == "n" && m.Value[1].ToString().ToLower() != "y")
                {
                    // 当該範囲置換
                    compositionBox.Text = text_all.Substring(0, m.Index)
                        + "ん"
                        + text_all.Substring(text1.Length - 1);

                    // キャレット位置設定
                    compositionBox.SelectionStart = compositionBox.Text.Length;

                    // Socal IME へリクエスト
                    //RequestSocialIME();
                    m_timer.Change(CompositionDelay, Timeout.Infinite);
                }
            }
        }

        private void RequestSocialIME()
        {
            Dispatcher.BeginInvoke(() =>
            {
                m_requestQueue.Add("http://www.social-ime.com/api2/predict.php?string=" + Uri.EscapeDataString(compositionBox.Text));
                if (m_requestQueue.Count == 1)
                {
                    m_client.DownloadStringAsync(new Uri(m_requestQueue[0]), m_requestQueue[0]);
                }
                else if (m_requestQueue.Count > 2)
                {
                    m_requestQueue.RemoveAt(1);
                }
            });
        }

        private void compositionBox_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Back:
                    if (compositionBox.Text == String.Empty)
                    {
                        CloseConjecture();
                    }
                    else
                    {
                        //RequestSocialIME();
                        m_timer.Change(CompositionDelay, Timeout.Infinite);
                    }
                    break;
                case Key.Enter:
                    decideToText(compositionBox.Text);
                    break;
            }
        }

        private void compositionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                decideToText(e.AddedItems[0] as string);
            }
        }

        private char keyToChar(Key key)
        {
            switch (key)
            {
                case Key.A: return 'A';
                case Key.Add: return '+';
                case Key.B: return 'B';
                case Key.C: return 'C';
                case Key.D: return 'D';
                case Key.D0: return '0';
                case Key.D1: return '1';
                case Key.D2: return '2';
                case Key.D3: return '3';
                case Key.D4: return '4';
                case Key.D5: return '5';
                case Key.D6: return '6';
                case Key.D7: return '7';
                case Key.D8: return '8';
                case Key.D9: return '9';
                case Key.Divide: return '/';
                case Key.E: return 'E';
                case Key.Enter: return '\n';
                case Key.F: return 'F';
                case Key.G: return 'G';
                case Key.H: return 'H';
                case Key.I: return 'I';
                case Key.J: return 'J';
                case Key.K: return 'K';
                case Key.L: return 'L';
                case Key.M: return 'M';
                case Key.Multiply: return '*';
                case Key.N: return 'N';
                case Key.NumPad0: return '0';
                case Key.NumPad1: return '1';
                case Key.NumPad2: return '2';
                case Key.NumPad3: return '3';
                case Key.NumPad4: return '4';
                case Key.NumPad5: return '5';
                case Key.NumPad6: return '6';
                case Key.NumPad7: return '7';
                case Key.NumPad8: return '8';
                case Key.NumPad9: return '9';
                case Key.O: return 'O';
                case Key.P: return 'P';
                case Key.Q: return 'Q';
                case Key.R: return 'R';
                case Key.S: return 'S';
                case Key.Space: return ' ';
                case Key.Subtract: return '-';
                case Key.T: return 'T';
                case Key.Tab: return '\t';
                case Key.U: return 'U';
                case Key.V: return 'V';
                case Key.W: return 'W';
                case Key.X: return 'X';
                case Key.Y: return 'Y';
                case Key.Z: return 'Z';
                default:
                    return '\0';
            }
        }

        private void OpenConjecture(string defaultStr)
        {
            compositionBox.Text = defaultStr.ToLower();
            compositionBox.SelectionStart = compositionBox.Text.Length;
            compositionBox.Visibility = System.Windows.Visibility.Visible;
            compositionList.ItemsSource = null;
            compositionListPopup.IsOpen = true;
            compositionList.MaxWidth = this.ActualWidth;
            compositionBox_TextChanged(compositionBox, null);
            compositionBox.Focus();
            if (StartComposition != null)
                StartComposition(this, new EventArgs());
        }

        private void CloseConjecture()
        {
            compositionBox.Visibility = System.Windows.Visibility.Collapsed;
            compositionBox.Text = "";
            compositionListPopup.IsOpen = false;
            textBox.Focus();
            if (EndComposition != null)
                EndComposition(this, new EventArgs());
        }

        private void decideToText(string text)
        {
            textBox.SelectedText = text;
            CloseConjecture();
            textBox.Focus();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Control_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            compositionList.MaxWidth = this.ActualWidth;
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TextChanged != null)
                TextChanged(this, e);
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs("Text"));
        }

        private void textBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (SelectionChanged != null)
                SelectionChanged(this, e);
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Text"));
                PropertyChanged(this, new PropertyChangedEventArgs("SelectedText"));
            }
        }
    }
}
