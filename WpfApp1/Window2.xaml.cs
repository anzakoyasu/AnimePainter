using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
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
    /// Window2.xaml の相互作用ロジック
    /// </summary>
    public partial class PlayAnime : Window
    {
        List<CanvasPage> canvasPage;
        Timer playPagesTimer = new Timer();

        int playingPage = 1;

        public PlayAnime()
        {
            InitializeComponent();
        }

        public PlayAnime(List<CanvasPage> _canvasPage)
        {

            InitializeComponent();
            canvasPage = _canvasPage;

            playPagesTimer.Elapsed += new ElapsedEventHandler(callback);
            playPagesTimer.Interval = 100;
            playPagesTimer.Start();
        }

        private void callback(object sender, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(PlayPages), null);

        }

        public void PlayPages()
        {
            inkCanvasLine.Strokes = canvasPage[playingPage - 1].StrokeLine;
            inkCanvasCoat.Strokes = canvasPage[playingPage - 1].StrokeCoat;

            playPagesTimer.Interval = canvasPage[playingPage - 1].Delay * 10;

            playingPage++;
            if (playingPage > canvasPage.Count)
            {
                playingPage = 1;
            }

        }
    }
}
