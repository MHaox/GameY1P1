using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AvondschoolGame;

public partial class MainWindow : Window
{
    private const int TileSize = 32;

    // # = muur, . = vloer, D = deur (op slot), K = sleutel
    private static readonly string[] Map =
    {
        "####################",
        "#........##........#",
        "#........##........#",
        "#....K...##........#",
        "#........##........#",
        "#........##........#",
        "####D###########D###",
        "#..................#",
        "#..................#",
        "#..................#",
        "####################",
    };

    private char[][] _tiles = null!;
    private readonly Dictionary<(int x, int y), Rectangle> _tileShapes = new();
    private Rectangle _player = null!;
    private int _px = 9, _py = 8;
    private int _keys;
    private bool _inQuiz;

    private static readonly Brush Floor = new SolidColorBrush(Color.FromRgb(0x1B, 0x26, 0x3B));
    private static readonly Brush Wall = new SolidColorBrush(Color.FromRgb(0x41, 0x5A, 0x77));
    private static readonly Brush Door = new SolidColorBrush(Color.FromRgb(0xFC, 0xA3, 0x11));
    private static readonly Brush Ghost = new SolidColorBrush(Color.FromRgb(0x00, 0xB4, 0xD8));
    private static readonly Brush FloorAlt = new SolidColorBrush(Color.FromRgb(0x20, 0x2E, 0x46));

    public MainWindow()
    {
        InitializeComponent();
    }

    // ---------- Menu ----------
    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        StartScreen.Visibility = Visibility.Collapsed;
        GameScreen.Visibility = Visibility.Visible;
        StartNewGame();
    }

    private void HelpButton_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show(
            "Loop met de pijltjestoetsen of WASD.\n" +
            "Loop tegen een deur om een som op te lossen en hem te openen.\n" +
            "Vind de sleutel en ontsnap uit de school!",
            "Hoe werkt het?");

    private void QuitButton_Click(object sender, RoutedEventArgs e) => Close();

    // ---------- Game setup ----------
    private void StartNewGame()
    {
        _tiles = Map.Select(r => r.ToCharArray()).ToArray();
        _keys = 0;
        _px = 9; _py = 8;
        _inQuiz = false;
        UpdateHud("Je wordt wakker in de mediatheek...");
        BuildMap();
    }

    private void BuildMap()
    {
        GameCanvas.Children.Clear();
        _tileShapes.Clear();

        int rows = _tiles.Length, cols = _tiles[0].Length;
        GameCanvas.Width = cols * TileSize;
        GameCanvas.Height = rows * TileSize;

        for (int y = 0; y < rows; y++)
            for (int x = 0; x < cols; x++)
            {
                var rect = new Rectangle { Width = TileSize, Height = TileSize };
                Canvas.SetLeft(rect, x * TileSize);
                Canvas.SetTop(rect, y * TileSize);
                GameCanvas.Children.Add(rect);
                _tileShapes[(x, y)] = rect;
                PaintTile(x, y);
            }

        // Speler (placeholder vierkantje, later vervangen door sprite)
        _player = new Rectangle
        {
            Width = TileSize - 8,
            Height = TileSize - 8,
            Fill = Ghost,
            RadiusX = 4,
            RadiusY = 4
        };
        GameCanvas.Children.Add(_player);
        PlacePlayer();
    }

    private void PaintTile(int x, int y)
    {
        var rect = _tileShapes[(x, y)];
        rect.Fill = _tiles[y][x] switch
        {
            '#' => Wall,
            'D' => Door,
            'K' => Door,   // sleutel: later eigen sprite
            _ => (x + y) % 2 == 0 ? Floor : FloorAlt
        };
        rect.RadiusX = rect.RadiusY = _tiles[y][x] == 'K' ? 12 : 0;
    }

    private void PlacePlayer()
    {
        Canvas.SetLeft(_player, _px * TileSize + 4);
        Canvas.SetTop(_player, _py * TileSize + 4);
    }

    // ---------- Input / movement ----------
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (GameScreen.Visibility != Visibility.Visible || _inQuiz) return;

        (int dx, int dy) = e.Key switch
        {
            Key.Up or Key.W => (0, -1),
            Key.Down or Key.S => (0, 1),
            Key.Left or Key.A => (-1, 0),
            Key.Right or Key.D => (1, 0),
            _ => (0, 0)
        };

        if (dx != 0 || dy != 0)
        {
            TryMove(_px + dx, _py + dy);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            GameScreen.Visibility = Visibility.Collapsed;
            StartScreen.Visibility = Visibility.Visible;
        }
    }

    private void TryMove(int nx, int ny)
    {
        char tile = _tiles[ny][nx];

        switch (tile)
        {
            case '#':
                return;

            case 'D':
                AskQuestionToOpenDoor(nx, ny);
                return;

            case 'K':
                _keys++;
                _tiles[ny][nx] = '.';
                PaintTile(nx, ny);
                UpdateHud("Je vond een sleutel!");
                break;
        }

        _px = nx; _py = ny;
        PlacePlayer();
    }

    // ---------- Quiz (stub) ----------
    private void AskQuestionToOpenDoor(int doorX, int doorY)
    {
        _inQuiz = true;

        // TODO: vervang door een echt quiz-paneel (taal / rekenen, turn-based)
        var result = MessageBox.Show("Wat is 7 x 8?\n\nJa = 56, Nee = 54",
                                     "Slot!", MessageBoxButton.YesNo);

        if (result == MessageBoxResult.Yes)
        {
            _tiles[doorY][doorX] = '.';
            PaintTile(doorX, doorY);
            UpdateHud("Goed! De deur gaat open.");
        }
        else
        {
            UpdateHud("Fout antwoord... de deur blijft dicht.");
        }

        _inQuiz = false;
    }

    private void UpdateHud(string message)
    {
        MessageText.Text = message;
        KeysText.Text = $"Sleutels: {_keys}";
    }
}