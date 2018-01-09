using System.Windows.Ink;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System;
using System.Timers;
using System.Windows.Input;
using Microsoft.Win32;
using System.IO;
using ImageMagick;
using CoreTweet;
using System.Runtime.InteropServices;

namespace WpfApp1
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        List<CanvasPage> canvasPage = new List<CanvasPage>();

        StrokeCollection copiedStrokes;

        Timer playPagesTimer = new Timer();

        PaginationT pagination;

        int playingPage = 1;
        private bool handleCtrl = true;

        int colorR = 0;
        int colorG = 0;
        int colorB = 0;

        const int LayerCoat = 2;
        const int LayerLine = 1;
        int currentLayer = LayerLine;


        public MainWindow()
        {
            InitializeComponent();
            inkCanvasLine.DefaultDrawingAttributes.Width = 0.5;
            inkCanvasLine.DefaultDrawingAttributes.Height = 0.5;
            inkCanvasCoat.DefaultDrawingAttributes.Width = 0.5;
            inkCanvasCoat.DefaultDrawingAttributes.Height = 0.5;

            inkCanvasLine.EditingMode = InkCanvasEditingMode.Ink;
            inkCanvasCoat.EditingMode = InkCanvasEditingMode.Ink;

            Console.WriteLine(WintabManager.IsWintabAvailable());
            CanvasPage page_first = new CanvasPage(new StrokeCollection(), new StrokeCollection());
            canvasPage.Add(page_first);

            widthLabel.Content = "線の太さ：" + 1;

            pagination = new PaginationT(pageButtonPanel);

            playPagesTimer.Elapsed += new ElapsedEventHandler(callback);
            playPagesTimer.Interval = 100;
            playPagesTimer.Start();

            inkCanvasLine.UseCustomCursor = true;
            inkCanvasLine.Cursor = Cursors.Pen;

            inkCanvasLine.Strokes.StrokesChanged += StrokesChangedEvent;
            inkCanvasLine.DefaultDrawingAttributes.FitToCurve = true;

            copiedStrokes = new StrokeCollection();

            sliderOpacity.Value = 0.2;
        }

        private void callback(object sender, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(PlayPages), null);

        }

        private void keyDownOP(object sender, KeyEventArgs e)
        {
            String key = (e.Key).ToString();
            ModifierKeys modifierKeys = Keyboard.Modifiers;

            int crPageIndex = pagination.getCurrentPageNo() - 1;
            if ((modifierKeys & ModifierKeys.Control) != ModifierKeys.None)
            {
                handleCtrl = false;
                if (key == "Z")
                {
                    if(currentLayer == LayerLine)
                    {
                        canvasPage[crPageIndex].UndoStroke(inkCanvasLine, "line");
                    }
                    else
                    {
                        canvasPage[crPageIndex].UndoStroke(inkCanvasCoat, "coat");
                    }

                }
                if (key == "Y")
                {
                    if (currentLayer == LayerLine)
                    {
                        canvasPage[crPageIndex].RedoStroke(inkCanvasLine, "line");
                    }
                    else
                    { 
                        canvasPage[crPageIndex].RedoStroke(inkCanvasCoat, "coat");
                    }
                }
                handleCtrl = true;
            }
        }

        public void StrokesChangedEvent(object sender, StrokeCollectionChangedEventArgs e)
        {
            if (!handleCtrl) { return; }
            

            int crPageIndex = pagination.getCurrentPageNo() - 1;
            if (currentLayer == LayerLine)
            {
                canvasPage[crPageIndex].PushChangedStroke("line", e);
            }
            else
            {
                canvasPage[crPageIndex].PushChangedStroke("coat", e);
            }
            

        }

        public void PlayPages()
        {
            canvasPlay.Children.Clear();

            DrawingVisual dv = new DrawingVisual();
            DrawingContext dc = dv.RenderOpen();

            Rect rectBounds;
            rectBounds = new Rect
            {
                Width = 450,
                Height = 450
            };

            dc.DrawRectangle(canvasPlay.Background, null, rectBounds);

            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)rectBounds.Width, (int)rectBounds.Height,
                96, 96,
                PixelFormats.Default);

            if (playingPage > pagination.getPageNum()) playingPage = pagination.getPageNum(); 

            if (playingPage == pagination.getCurrentPageNo())
            {
                inkCanvasCoat.Strokes.Draw(dc);
                inkCanvasLine.Strokes.Draw(dc);
            }
            else
            {
                canvasPage[playingPage - 1].StrokeCoat.Draw(dc);
                canvasPage[playingPage - 1].StrokeLine.Draw(dc);
            }
            dc.Close();
            rtb.Render(dv);
            // ビットマップフレームを作成
            BitmapSource s = BitmapFrame.Create(rtb);
            Image ig = new Image(); ig.Source = s;
            ig.Width = 240; ig.Height = 240;
            canvasPlay.Children.Add(ig);

            playPagesTimer.Interval = canvasPage[playingPage-1].Delay * 10;

            playingPage++;
            if(playingPage > pagination.getPageNum())
            {
                playingPage = 1;
            }
            
        }

        private void radioButtonChecked(object sender, RoutedEventArgs e)
        {
            inkCanvasLine.EditingMode = InkCanvasEditingMode.Ink;
            inkCanvasCoat.EditingMode = InkCanvasEditingMode.Ink;            
        }

        private void radioButtonChecked1(object sender, RoutedEventArgs e)
        {
            inkCanvasLine.EditingMode = InkCanvasEditingMode.EraseByPoint;
            inkCanvasCoat.EditingMode = InkCanvasEditingMode.EraseByPoint;
        }

        
        private string WriteGif(int delay = 10)
        {

            string[] imageFiles = new string[canvasPage.Count];
            DateTime nowtime = DateTime.Now;
            string nowts = nowtime.ToString("yyyyMMddHHmmss");
            //string savePath;
            for (var i = 0; i < canvasPage.Count; i++)
            {
                DrawingVisual dv = new DrawingVisual();
                DrawingContext dc = dv.RenderOpen();

                Rect rectBounds;
                rectBounds = new Rect
                {
                    Width = 450,
                    Height = 450
                };

                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(255,255,255)), null, rectBounds);

                RenderTargetBitmap rtb = new RenderTargetBitmap(
                    (int)rectBounds.Width, (int)rectBounds.Height,
                    96, 96,
                    PixelFormats.Default);

                if(i+1 == pagination.getCurrentPageNo())
                {
                    inkCanvasCoat.Strokes.Draw(dc);
                    inkCanvasLine.Strokes.Draw(dc);
                }
                else
                {
                    canvasPage[i].StrokeCoat.Draw(dc);
                    canvasPage[i].StrokeLine.Draw(dc);
                }
                
                dc.Close();
                rtb.Render(dv);
                // ビットマップフレームを作成
                PngBitmapEncoder gifenc = new PngBitmapEncoder();
                gifenc.Frames.Add(BitmapFrame.Create(rtb));
                
                System.IO.Directory.CreateDirectory(nowts);
                System.IO.Stream stream = System.IO.File.Create(nowts + "/"+ i + ".gif");
                gifenc.Save(stream);
                stream.Close();

                imageFiles[i] = nowts +"/"+ i + ".gif";
            }

            using (MagickImageCollection collection = new MagickImageCollection())
            {
                
                for(int i=0; i < canvasPage.Count; i++)
                {
                    collection.Add(nowts + "/" + i + ".gif");
                    collection[i].AnimationDelay = delay;
                }
 
                // Optionally reduce colors
                QuantizeSettings settings = new QuantizeSettings();
                settings.Colors = 256;
                collection.Quantize(settings);

                // Optionally optimize the images (images should have the same size).
                collection.Optimize();

                // Save gif
                collection.Write(nowts + ".gif");
            }
            return nowts + ".gif";
        }

        private void UploadGifToTwitter(string filename)
        {
            //var s = OAuth.Authorize("nIQvaoLZJGDbrONRbtdfkCClV", "IBqijdZsH8JOG5z5M1EAhGSdGUsHyxCOK4NOWyxXz3I9vF245w");
            //System.Diagnostics.Process.Start(s.AuthorizeUri.AbsoluteUri);
            //var url = s.AuthorizeUri;
            //Console.WriteLine("access here: {0}", s.AuthorizeUri);
            //var t = OAuth.GetTokens(s, "PIN code");//s.GetTokens("PINCODE");

            var t = CoreTweet.Tokens.Create("nIQvaoLZJGDbrONRbtdfkCClV", "IBqijdZsH8JOG5z5M1EAhGSdGUsHyxCOK4NOWyxXz3I9vF245w", "1316448422-cBkHkYPsVxwAxqjdVwOcapF1jArGmtnLtxW3ZmX", "RcCaVMkahGRr1GSNMdm85Pn39oybxexGYAt4ENdLrO3P5");

            MediaUploadResult image = t.Media.Upload(media: new FileInfo(filename));

            try
            {
                Status st = t.Statuses.Update(
                    status: "gif_anime",
                    media_ids: new long[] { image.MediaId }
                );
                MessageBox.Show("アップロードされました");
            }
            catch (TwitterException e)
            {
                MessageBox.Show("接続エラー");
            }
            //upload saremasita!

        }

        private void buttonPageClick(object sender, RoutedEventArgs e)
        {
            int beforePageNo  = pagination.getCurrentPageNo();

            pagination.HandlePagination(sender, e);

            int currentPageNo = pagination.getCurrentPageNo();

            if (pagination.IsRemoved())
            {
                canvasPage.RemoveAt(beforePageNo - 1);

                inkCanvasLine.Strokes = canvasPage[currentPageNo - 1].StrokeLine;
                inkCanvasCoat.Strokes = canvasPage[currentPageNo - 1].StrokeCoat;
            }

            if (pagination.IsAdded())
            {
                canvasPage.Insert(currentPageNo, new CanvasPage(new StrokeCollection(), new StrokeCollection()));
            }
            if(beforePageNo <= pagination.getPageNum())
            {
                canvasPage[beforePageNo - 1].StrokeLine = inkCanvasLine.Strokes;
                canvasPage[beforePageNo - 1].StrokeCoat = inkCanvasCoat.Strokes;
            }

            if (currentPageNo > 1)
            {
                inkCanvasPreLine.Visibility = Visibility.Visible;
                inkCanvasPreCoat.Visibility = Visibility.Visible;
                inkCanvasPreLine.Strokes = canvasPage[currentPageNo - 2].StrokeLine.Clone();
                inkCanvasPreCoat.Strokes = canvasPage[currentPageNo - 2].StrokeCoat.Clone();
            }
            else
            {
                inkCanvasPreLine.Visibility = Visibility.Hidden;
                inkCanvasPreCoat.Visibility = Visibility.Hidden;

            }
            inkCanvasLine.Strokes = canvasPage[currentPageNo - 1].StrokeLine;
            inkCanvasCoat.Strokes = canvasPage[currentPageNo - 1].StrokeCoat;

            if (currentLayer == LayerLine)
            {
                inkCanvasLine.Strokes.StrokesChanged += StrokesChangedEvent;
            }
            else
            {
                inkCanvasCoat.Strokes.StrokesChanged += StrokesChangedEvent;
            }

        }

        private void CurrentLayerClear()
        {
            if (currentLayer == LayerLine)
            {
                inkCanvasLine.Strokes.Clear();
            }
            else
            {
                inkCanvasCoat.Strokes.Clear();
            }
        }

        private void MenuItemClick(Object sender, RoutedEventArgs e)
        {
            MenuItem menuitem = (MenuItem)sender;
            string tag = menuitem.Tag.ToString();

            switch (tag)
            {
                case "new":
                    break;
                case "save":
                    SaveStrokesToISF();
                    break;
                case "openISF":
                    OpenISFFile();
                    break;
                case "writeGif":
                    WriteGif();
                    break;
                case "allClear":
                    break;
                case "currentClear":
                    CurrentLayerClear();
                    break;
                case "upload":
                    UploadGifToTwitter(WriteGif());
                    break;
                case "pageDelay":
                    PageDelayWindow pageDelayw = new PageDelayWindow(canvasPage);
                    pageDelayw.Show();
                    break;
                case "playView":
                    PlayAnime playAnime = new PlayAnime(canvasPage);
                    playAnime.Show();
                    break;
                default:
                    break;
            }
        }

        private void SaveStrokesToISF()
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Filter = "Ink ファイル(.isf)|*.isf";
            fileDialog.DefaultExt = "isf";
            Nullable<bool> result = fileDialog.ShowDialog();

            if (result != true) return;

            DateTime nowtime = DateTime.Now;
            string fileName = fileDialog.FileName;

            for(int i=0; i < canvasPage.Count; i++) {
                string linefile = fileName.Replace(".isf", "l" + i + ".isf");
                FileStream fs = new FileStream(linefile, FileMode.Create);
                canvasPage[i].StrokeLine.Save(fs);

                string coatfile = fileName.Replace(".isf", "c" + i + ".isf");
                fs = new FileStream(coatfile, FileMode.Create);
                canvasPage[i].StrokeCoat.Save(fs);
                fs.Close();
            }

        }

        private void OpenISFFile()
        {

        }

        private void sliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int width = (int)e.NewValue + 1;
            widthLabel.Content = "線の太さ："+ width;
            inkCanvasLine.DefaultDrawingAttributes.Width = width/2.0;
            inkCanvasLine.DefaultDrawingAttributes.Height = width/2.0;
            inkCanvasCoat.DefaultDrawingAttributes.Width = width/2.0;
            inkCanvasCoat.DefaultDrawingAttributes.Height = width/2.0;

        }

        private void radioButtonChecked2(object sender, RoutedEventArgs e)//線画レイヤー
        {
            if(currentLayer == LayerCoat) //以前は塗りレイヤー
            {
                inkCanvasLine.IsHitTestVisible = true;
                inkCanvasCoat.IsHitTestVisible = false;
                
                currentLayer = LayerLine;
                layerLabel.Content = "線画";

                inkCanvasLine.Strokes.StrokesChanged += StrokesChangedEvent;
                inkCanvasCoat.Strokes.StrokesChanged -= StrokesChangedEvent;

            }
        }

        private void radioButtonChecked3(object sender, RoutedEventArgs e)//塗りレイヤー
        {
 
            if (currentLayer == LayerLine) //以前は線画レイヤー
            {
                inkCanvasLine.IsHitTestVisible = false;
                inkCanvasCoat.IsHitTestVisible = true;
                 
                currentLayer = LayerCoat;
                layerLabel.Content = "塗り";

                inkCanvasLine.Strokes.StrokesChanged -= StrokesChangedEvent;
                inkCanvasCoat.Strokes.StrokesChanged += StrokesChangedEvent;
            }
        }

        private void ColorSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider s = (Slider)e.Source;
            string tag = s.Tag.ToString();

            switch (tag)
            {
                case "r":
                    colorR = (int)s.Value;
                    break;
                case "g":
                    colorG = (int)s.Value;
                    break;
                case "b":
                    colorB = (int)s.Value;
                    break;
            }
            setColor();
        }

        private void setColor()
        {
            Color penColor = Color.FromArgb(255, Convert.ToByte(colorR), Convert.ToByte(colorG), Convert.ToByte(colorB));
            colorView.Fill = new SolidColorBrush(penColor);
            inkCanvasLine.DefaultDrawingAttributes.Color = penColor;
            inkCanvasCoat.DefaultDrawingAttributes.Color = penColor;

        }

        private void sliderSetOpacity(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider s = (Slider)e.Source;
            inkCanvasPreLine.Opacity = s.Value;
            inkCanvasPreCoat.Opacity = s.Value;
            labelOpacity.Content = "透明度 " + (int)(s.Value*100);
        }

        private void buttonCopyClick(object sender, RoutedEventArgs e)
        {
            if (currentLayer == LayerCoat)
            {
                copiedStrokes = inkCanvasCoat.Strokes.Clone();
            }
            else
            {
                copiedStrokes = inkCanvasLine.Strokes.Clone();
            }
        }

        private void buttonPasteClick(object sender, RoutedEventArgs e)
        {

            if (currentLayer == LayerCoat)
            {//重くなる
                inkCanvasCoat.Strokes.Add(copiedStrokes.Clone());
            }
            else
            {
                inkCanvasLine.Strokes.Add(copiedStrokes.Clone());
            }
        }

        public class PaginationT
        {
            Panel root;
            int currentPageNo = 1;
            int pageNum = 1;
            const int MaxPageNum = 100;

            bool isAdded = false;
            bool isRemoved = false;

            Button bpx1 = new Button(), bpx9 = new Button();

            public PaginationT(Panel root)
            {
                this.root = root;
                int i = 0;
                foreach (var b in LogicalTreeHelper.GetChildren(root))
                {
                    if (i == 2) bpx1 = (Button)b;
                    if (i == 10) bpx9 = (Button)b;
                    i++;
                }
            }

            public int getPageNum()
            {
                return pageNum;
            }

            public bool IsAdded()
            {
                return isAdded;
            }

            public bool IsRemoved()
            {
                return isRemoved;
            }

            public int getCurrentPageNo()
            {
                return currentPageNo;
            }

            public void HandlePagination(object sender, RoutedEventArgs e)
            {
                isAdded = false;
                isRemoved = false;
                Button clickedButton = (Button)e.Source;
                String btnName = clickedButton.Name;

                if (btnName == "prev")
                {
                    Prev(sender, e);
                }
                else if (btnName == "next")
                {
                    Next(sender, e);
                }
                else if (btnName == "minus")
                {
                    RemovePage(sender, e);
                }
                else if (btnName == "plus")
                {
                    AddPage(sender, e);
                }
                else
                {
                    int pageNo = Convert.ToInt16(clickedButton.Content);
                    currentPageNo = pageNo;
                }


                Reflect(sender, e);

            }

            public void Prev(object sender, RoutedEventArgs e)
            {
                if (currentPageNo > 1)
                {
                    currentPageNo--;
                    SetButtonContentPrev(sender, e);
                }
            }

            public void Next(object sender, RoutedEventArgs e)
            {
                if (currentPageNo < pageNum)
                {
                    currentPageNo++;
                    if (currentPageNo > Convert.ToInt16((String)bpx9.Content))
                    {
                        int i = 0;
                        foreach (var b in LogicalTreeHelper.GetChildren(root))
                        {
                            if (i < 2 || i > 10)
                            {
                                i++;
                                continue;
                            }
                            Button bpxi = (Button)b;
                            bpxi.Content = (currentPageNo + (i - 2)).ToString();
                            i++;
                        }
                    }
                }
            }

            public void Reflect(object sender, RoutedEventArgs e)
            {
                int index = 0;
                foreach (var b in LogicalTreeHelper.GetChildren(root))
                {
                    if (index < 2 || index > 10)
                    {
                        index++;
                        continue;
                    }
                    Button bpxi = (Button)b;
                    int page = Convert.ToInt32(bpxi.Content);

                    bpxi.Background = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                    if (page == currentPageNo)
                    {
                        bpxi.Background = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                    }

                    if (page <= pageNum)
                    {
                        bpxi.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        bpxi.Visibility = Visibility.Collapsed;
                    }
                    index++;
                }
            }


            public void AddPage(object sender, RoutedEventArgs e)
            {
                if (pageNum < MaxPageNum)
                {
                    pageNum++;
                    isAdded = true;
                }
            }


            public void RemovePage(object sender, RoutedEventArgs e)
            {
                if (pageNum > 1)//
                {
                    pageNum--;
                    if (currentPageNo > pageNum)
                    {
                        currentPageNo = pageNum;
                    }
                    isRemoved = true;

                    SetButtonContentPrev(sender, e);
                }
            }

            public void SetButtonContentPrev(object sender, RoutedEventArgs e)
            {
                if (currentPageNo < Convert.ToInt16((String)bpx1.Content))
                {
                    int i = 0;
                    foreach (var b in LogicalTreeHelper.GetChildren(root))
                    {
                        if (i < 2 || i > 10)
                        {
                            i++;
                            continue;
                        }
                        Button bpxi = (Button)b;
                        bpxi.Content = (currentPageNo - 9 + (i - 1)).ToString();
                        i++;
                    }
                }
            }

        }

        class WintabFunctions
        {
            [DllImport("wintab32.dll",CharSet=CharSet.Auto)]
            public static extern UInt32 WTInfoA(UInt32 wCategory, UInt32 nIndex, IntPtr lpOutput);
        }

        class WintabStructs
        {
            public enum EWTICategoryIndex
            {
                WTI_INTERFACE = 1,
                WTI_STATUS = 2,
                WTI_DEFCONTEXT = 3,
                WTI_DEFSYSCTX = 4,
                WTI_DEVICES = 100,
                WTI_CURSORS = 200,
                WTI_EXTENSIONS = 300,
                WTI_DDCTXS = 400,
                WTI_DSCTXS = 500
            }

            public enum EWTIDevicesIndex
            {
                DVC_NAME = 1,
                DVC_HARDWARE = 2,
                DVC_NCSRTYPES = 3,
                DVC_FIRSTCSR = 4,
                DVC_PKTRATE = 5,
                DVC_PKTDATA = 6,
                DVC_PKTMODE = 7,
                DVC_CSRDATA = 8,
                DVC_XMARGIN = 9,
                DVC_YMARGIN = 10,
                DVC_ZMARGIN = 11,
                DVC_X = 12,
                DVC_Y = 13,
                DVC_Z = 14,
                DVC_NPRESSURE=15,
                DVC_TPRESSURE=16,
                DVC_ORIENTATION = 17,
                DVC_ROTATION=18,
                DVC_PNPID = 19
            }

            public struct WintabAxis
            {
                public Int32 axMin;
                public Int32 axMax;
                public UInt32 axUnits;
                public UInt32 axResolusion;
            }
        }

        class WintabManager
        {
            WintabManager()
            {
                Console.Write(IsWintabAvailable());
            }

            public static bool IsWintabAvailable()
            {
                try
                {
                    uint result = WintabFunctions.WTInfoA(0, 0, IntPtr.Zero);
                    if(result > 0)
                    {
                        return true;
                    }

                    return false;
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("WintabManager IsWintabAvailable : " + ex.Message);
                    return false;
                }
            }
        }

        public class CanvasPage
        {
            private StrokeCollection strokesLine;
            private StrokeCollection strokesCoat;

            private List<StrokeCollection> addedLine;
            private List<StrokeCollection> addedCoat;
            private List<StrokeCollection> removedLine;
            private List<StrokeCollection> removedCoat;

            private int delay = 10;

            private int stack_i_line;
            private int stack_i_coat;

            public CanvasPage(StrokeCollection scl, StrokeCollection scc)
            {
                this.strokesLine = scl;
                this.strokesCoat = scc;
                this.addedLine = new List<StrokeCollection>();
                this.addedCoat = new List<StrokeCollection>();
                this.removedLine = new List<StrokeCollection>();
                this.removedCoat = new List<StrokeCollection>();

                this.stack_i_line = 0;
                this.stack_i_coat = 0;
            }

            public int Delay
            {
                set { this.delay = value; }
                get { return delay; }
            }

            public StrokeCollection StrokeLine
            {
                set { this.strokesLine = value; }
                get { return strokesLine; }
            }
            public StrokeCollection StrokeCoat
            {
                set { this.strokesCoat = value; }
                get { return strokesCoat; } 
            }

            public List<StrokeCollection> AddedStrokesLine
            {
                set { this.addedLine = value; }
                get { return addedLine; }
            }
            public List<StrokeCollection> AddedStrokesCoat
            {
                set { this.addedCoat = value; }
                get { return addedCoat; }
            }
            public List<StrokeCollection> RemovedStrokesLine
            {
                set { this.removedLine = value; }
                get { return removedLine; }
            }
            public List<StrokeCollection> RemovedStrokesCoat
            {
                set { this.removedCoat = value; }
                get { return removedCoat; }
            }

            public void PushChangedStroke(String type, StrokeCollectionChangedEventArgs e)
            {
                if(type == "line")
                {
                    push(addedLine, removedLine, stack_i_line, e);
                    stack_i_line = 0;
                }
                else
                {
                    push(addedCoat, removedCoat, stack_i_coat, e);
                    stack_i_coat = 0;
                }   
            }

            private void push(List<StrokeCollection> _added, List<StrokeCollection> _removed, int stack_i,StrokeCollectionChangedEventArgs e)
            {
                _added.Insert(_added.Count - stack_i, e.Added);
                _removed.Insert(_removed.Count - stack_i, e.Removed);

                int bcount = _added.Count;
                for (int i = _added.Count - stack_i; i < bcount; i++)
                {
                    _added.RemoveAt(bcount - stack_i);
                }

                bcount = _removed.Count;
                for (int i = _removed.Count - stack_i; i < bcount; i++)
                {
                    _removed.RemoveAt(bcount - stack_i);
                }
            }

            public void UndoStroke(InkCanvas inkCanvas_changed, String type)
            {
                if(type == "line") {
                    stack_i_line = undo(addedLine, removedLine, stack_i_line, inkCanvas_changed);
                }
                else
                {
                    stack_i_coat = undo(addedCoat, removedCoat, stack_i_coat, inkCanvas_changed);
                }
                
            }

            public void RedoStroke(InkCanvas inkCanvas_changed, String type)
            {
                if (type == "line")
                {
                    stack_i_line = redo(addedLine, removedLine, stack_i_line, inkCanvas_changed);
                }
                else
                {
                    stack_i_coat = redo(addedCoat, removedCoat, stack_i_coat, inkCanvas_changed);
                }
            }


            private int undo(List<StrokeCollection> _added, List<StrokeCollection> _removed, int stack_i, InkCanvas inkCanvas_changed)
            {
                if (stack_i == _added.Count)//もうUndoできない
                {
                    return stack_i;
                }

                try
                {
                    inkCanvas_changed.Strokes.Remove(_added[ _added.Count - 1 - stack_i]);
                    inkCanvas_changed.Strokes.Add(_removed[_removed.Count - 1 - stack_i]);

                    stack_i++;
                    return stack_i;
                }
                catch (ArgumentException)
                {
                    return stack_i;
                }
            }

            private int redo(List<StrokeCollection> _added, List<StrokeCollection> _removed, int stack_i, InkCanvas inkCanvas_changed)
            {
                stack_i--;
                if (stack_i < 0)
                {
                    stack_i = 0;
                    return stack_i;
                }

                inkCanvas_changed.Strokes.Add(_added[_added.Count - 1 - stack_i]);
                inkCanvas_changed.Strokes.Remove(_removed[_removed.Count - 1 - stack_i]);

                return stack_i;
            }
        }

        private void buttonClickScaling(object sender, RoutedEventArgs e)
        {
            Button clickedButton = (Button)e.Source;
            String btnName = clickedButton.Content.ToString();

            if(btnName == "+")
            {
                ScaleXY.ScaleX *= 2;
                ScaleXY.ScaleY *= 2;
                inkCanvasLine.DefaultDrawingAttributes.Width *= Math.Sqrt(1.5);
                inkCanvasCoat.DefaultDrawingAttributes.Width *= Math.Sqrt(1.5);
                inkCanvasLine.DefaultDrawingAttributes.Height *= Math.Sqrt(1.5);
                inkCanvasCoat.DefaultDrawingAttributes.Height *= Math.Sqrt(1.5);

            }
            else if(btnName == "-")
            {
                ScaleXY.ScaleX /= 2;
                ScaleXY.ScaleY /= 2;
                inkCanvasLine.DefaultDrawingAttributes.Width /= Math.Sqrt(1.5);
                inkCanvasCoat.DefaultDrawingAttributes.Width /= Math.Sqrt(1.5);
                inkCanvasLine.DefaultDrawingAttributes.Height /= Math.Sqrt(1.5);
                inkCanvasCoat.DefaultDrawingAttributes.Height /= Math.Sqrt(1.5);
            }
        }
    }

    
}
