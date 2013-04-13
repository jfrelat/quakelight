using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.IO;

namespace InnoveWare
{
    public partial class Page : UserControl
    {
        private int _frames_count = 0;
        private int _last_frame = 0;
        private DateTime _lastTime;
        public static BitmapImage bitmap = new BitmapImage();
        private Dictionary<Key, int> dictionaryKeys;
        private double oldtime;
        public static Page thePage;
        public static int gwidth;
        public static int gheight;
        private int count_fps = 0;
        private int nb_fps = 0;

        public Page()
        {
            // Required to initialize variables
            InitializeComponent();
            thePage = this;
        }

        void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Set focus on the page
            this.Focus();

            // Auto scale the rendering screen.
            gwidth = (int)Application.Current.Host.Content.ActualWidth;
            gheight = (int)Application.Current.Host.Content.ActualHeight;

            // Prepare rendering image.
            image.Width = gwidth;
            image.Height = gheight;
            image.Source = bitmap;
            
            // Game loop.
            CompositionTarget.Rendering += new EventHandler(Page_CompositionTarget_Rendering);

            // FPS counter timer.
            fps_timer.Completed += new EventHandler(Page_FpsCounter);
            _lastTime = DateTime.Now;
            fps_timer.Begin();

            // Initialize Quake.
            oldtime = quake.sys_linux.Sys_FloatTime();
            quake.sys_linux.main (0, null);

            // Prepare keyboard.
            BuildDictionaryKeys();
            KeyDown += new KeyEventHandler(Page_KeyDown);

            // Prepare full screen support.
            image.MouseLeftButtonDown += new MouseButtonEventHandler(Page_MouseLeftButtonDown);
            Application.Current.Host.Content.FullScreenChanged += new EventHandler(Page_FullScreenChanged);
        }

        /// <summary>
        /// Mouse left button down event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Page_MouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            Application.Current.Host.Content.IsFullScreen = !Application.Current.Host.Content.IsFullScreen;
        }

        /// <summary>
        /// Full screen changed event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Page_FullScreenChanged(object sender, EventArgs e)
        {
            // Auto scale the rendering screen.
            image.Width = Application.Current.Host.Content.ActualWidth;
            image.Height = Application.Current.Host.Content.ActualHeight;
        }

        /// <summary>
        /// Build a dictionary to map Silverlight keys to Quake keys.
        /// </summary>
        private void BuildDictionaryKeys()
        {
            // Create the dictionary.
            dictionaryKeys = new Dictionary<Key, int>();

            // Add special key mappings.
            dictionaryKeys.Add(Key.Escape, quake.keys.K_ESCAPE);
            dictionaryKeys.Add(Key.Up, quake.keys.K_UPARROW);
            dictionaryKeys.Add(Key.Down, quake.keys.K_DOWNARROW);
            dictionaryKeys.Add(Key.Left, quake.keys.K_LEFTARROW);
            dictionaryKeys.Add(Key.Right, quake.keys.K_RIGHTARROW);
            dictionaryKeys.Add(Key.Enter, quake.keys.K_ENTER);
            dictionaryKeys.Add(Key.Back, quake.keys.K_BACKSPACE);
            dictionaryKeys.Add(Key.Home, quake.keys.K_HOME);
            dictionaryKeys.Add(Key.End, quake.keys.K_END);
            dictionaryKeys.Add(Key.Tab, quake.keys.K_TAB);
            dictionaryKeys.Add(Key.PageUp, quake.keys.K_PGUP);
            dictionaryKeys.Add(Key.PageDown, quake.keys.K_PGDN);
            dictionaryKeys.Add(Key.Space, quake.keys.K_SPACE);

            // Add default key mappings.
            dictionaryKeys.Add(Key.D0, '0');
            dictionaryKeys.Add(Key.D1, '1');
            dictionaryKeys.Add(Key.D2, '2');
            dictionaryKeys.Add(Key.D3, '3');
            dictionaryKeys.Add(Key.D4, '4');
            dictionaryKeys.Add(Key.D5, '5');
            dictionaryKeys.Add(Key.D6, '6');
            dictionaryKeys.Add(Key.D7, '7');
            dictionaryKeys.Add(Key.D8, '8');
            dictionaryKeys.Add(Key.D9, '9');
            dictionaryKeys.Add(Key.NumPad0, '0');
            dictionaryKeys.Add(Key.NumPad1, '1');
            dictionaryKeys.Add(Key.NumPad2, '2');
            dictionaryKeys.Add(Key.NumPad3, '3');
            dictionaryKeys.Add(Key.NumPad4, '4');
            dictionaryKeys.Add(Key.NumPad5, '5');
            dictionaryKeys.Add(Key.NumPad6, '6');
            dictionaryKeys.Add(Key.NumPad7, '7');
            dictionaryKeys.Add(Key.NumPad8, '8');
            dictionaryKeys.Add(Key.NumPad9, '9');
            dictionaryKeys.Add(Key.A, 'a');
            dictionaryKeys.Add(Key.B, 'b');
            dictionaryKeys.Add(Key.C, 'c');
            dictionaryKeys.Add(Key.D, 'd');
            dictionaryKeys.Add(Key.E, 'e');
            dictionaryKeys.Add(Key.F, 'f');
            dictionaryKeys.Add(Key.G, 'g');
            dictionaryKeys.Add(Key.H, 'h');
            dictionaryKeys.Add(Key.I, 'i');
            dictionaryKeys.Add(Key.J, 'j');
            dictionaryKeys.Add(Key.K, 'k');
            dictionaryKeys.Add(Key.L, 'l');
            dictionaryKeys.Add(Key.M, 'm');
            dictionaryKeys.Add(Key.N, 'n');
            dictionaryKeys.Add(Key.O, 'o');
            dictionaryKeys.Add(Key.P, 'p');
            dictionaryKeys.Add(Key.Q, 'q');
            dictionaryKeys.Add(Key.R, 'r');
            dictionaryKeys.Add(Key.S, 's');
            dictionaryKeys.Add(Key.T, 't');
            dictionaryKeys.Add(Key.U, 'u');
            dictionaryKeys.Add(Key.V, 'v');
            dictionaryKeys.Add(Key.W, 'w');
            dictionaryKeys.Add(Key.X, 'x');
            dictionaryKeys.Add(Key.Y, 'y');
            dictionaryKeys.Add(Key.Z, 'z');
            dictionaryKeys.Add(Key.Divide, '/');
        }

        /// <summary>
        /// Keydown event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            // Forward translated key to quake keyboard event.
            int quake_key;
            if (dictionaryKeys.TryGetValue(e.Key, out quake_key))
                quake.keys.Key_Event(quake_key, true);
            else if (e.PlatformKeyCode == 0xbc)
                quake.keys.Key_Event(0x2c, true);
        }

        /// <summary>
        /// Silverlight UI thread (main game loop).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Page_CompositionTarget_Rendering(object sender, EventArgs e)
        {
            // Count frame.
            _frames_count++;

            // Synchronize the Silverlight UI thread to Quake framerate.
		    double newtime = quake.sys_linux.Sys_FloatTime ();
            double time = newtime - oldtime;
            quake.host.Host_Frame(time);
            oldtime = newtime;
        }

        /// <summary>
        /// FPS counter.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Page_FpsCounter(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            TimeSpan dt = ((TimeSpan)(now.Subtract(_lastTime)));
            int _cFrames = _frames_count - _last_frame;
            int _cFps = (int)((_frames_count - _last_frame) / (dt.Seconds + dt.Milliseconds / 1000.0));
            count_fps += _cFps;
            nb_fps++;
            fps.Foreground = new SolidColorBrush(Colors.White);
            fps.Text = (count_fps / nb_fps).ToString() + " fps";
            _last_frame = _frames_count;
            _lastTime = now;

            // Restart the timer.
            fps_timer.Begin();
        }
    }
}
