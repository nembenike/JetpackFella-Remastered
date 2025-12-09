using JetpackFella_Remastered;
using System.Windows;
using System.Windows.Input;

namespace JetpackFella_Remastered
{
    public partial class MainWindow : Window
    {
        private Game _game;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _game = new Game(GameCanvas, (int)ActualWidth, (int)ActualHeight);
            GameCanvas.Focus();
            _game.Start();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            _game?.KeyDown(e.Key);
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            _game?.KeyUp(e.Key);
        }
    }
}