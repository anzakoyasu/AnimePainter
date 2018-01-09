using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static WpfApp1.MainWindow;

namespace WpfApp1
{
    /// <summary>
    /// Window1.xaml の相互作用ロジック
    /// </summary>
    public partial class PageDelayWindow : Window
    {
        List<CanvasPage> canvasPage;
        List<TextBox> delayboxs;
        public PageDelayWindow()
        {
            InitializeComponent();
        }

        public PageDelayWindow(List<CanvasPage> _canvasPage)
        {
            delayboxs = new List<TextBox>();
            InitializeComponent();
            canvasPage = _canvasPage;
            pageGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(40) });
            pageGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(100) });
            for (int i=0; i < canvasPage.Count; i++)
            {
                pageGrid.Height = (i + 1) * 40;

                pageGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                var pagelabel = new Label();
                pagelabel.Content = i+1;
                pagelabel.SetValue(Grid.RowProperty, i);
                pagelabel.SetValue(Grid.ColumnProperty, 0);
                pageGrid.Children.Add(pagelabel);

                var delayTextBox = new TextBox() {};
                delayTextBox.Text = canvasPage[i].Delay.ToString();
                delayTextBox.Height =30;
                delayTextBox.SetValue(Grid.RowProperty, i);
                delayTextBox.SetValue(Grid.ColumnProperty, 1); 
                delayTextBox.PreviewTextInput += textBoxPrice_PreviewTextInput;
                pageGrid.Children.Add(delayTextBox);
                delayboxs.Add(delayTextBox);
            }
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < canvasPage.Count; i++)
            {
                int delay = int.Parse(delayboxs[i].Text);
                if(delay <= 0)
                {
                    MessageBox.Show("0より大きい数値を入れてください");
                    return;
                }
                canvasPage[i].Delay = delay;
                Console.WriteLine(canvasPage[i].Delay);
            }
            this.Close();
        }

        private void textBoxPrice_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 0-9のみ
            e.Handled = !new Regex("[0-9]").IsMatch(e.Text);

        }

        private void textBoxPrice_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            // 貼り付けを許可しない
            if (e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }
    }
}
