using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AvondschoolGame;

public partial class MainWindow : Window
{
    // Isometric dimensions for proper diamond tiles
    private const int TileWidth = 64;   // Width of isometric tile
    private const int TileHeight = 32;  // Height of isometric tile
    private const int OffsetX = 50;    // X offset to center view
    private const int OffsetY = 300;    // Y offset to center view

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
    private readonly Dictionary<(int x, int y), Polygon> _tileShapes = new();
    private Ellipse _player = null!;
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

        // Canvas size based only on grid dimensions, not affected by offset
        GameCanvas.Width = (cols + rows) * (TileWidth / 2) + 100;
        GameCanvas.Height = (cols + rows) * (TileHeight / 2) + 100;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                // Create isometric diamond polygon
                var polygon = CreateIsometricTile();

                // Calculate isometric position (rotated 90 degrees clockwise)
                int isoX = (y + x) * (TileWidth / 2) + OffsetX;
                int isoY = (y - x) * (TileHeight / 2) + OffsetY;

                Canvas.SetLeft(polygon, isoX);
                Canvas.SetTop(polygon, isoY);

                // Z-index for depth sorting
                Panel.SetZIndex(polygon, x + y);

                GameCanvas.Children.Add(polygon);
                _tileShapes[(x, y)] = polygon;
                PaintTile(x, y);
            }
        }

        // Add player as ellipse
        _player = new Ellipse
        {
            Width = TileWidth / 2,
            Height = TileHeight,
            Fill = Ghost
        };
        GameCanvas.Children.Add(_player);
        PlacePlayer();
    }

    private Polygon CreateIsometricTile()
    {
        // Create a diamond shape for isometric tile
        var polygon = new Polygon
        {
            Points = new PointCollection
            {
                new Point(TileWidth / 2, 0),              // Top
                new Point(TileWidth, TileHeight / 2),     // Right
                new Point(TileWidth / 2, TileHeight),     // Bottom
                new Point(0, TileHeight / 2)               // Left
            },
            StrokeThickness = 1,
            Stroke = new SolidColorBrush(Color.FromRgb(0x1B, 0x26, 0x3B))
        };
        return polygon;
    }

    private void PaintTile(int x, int y)
    {
        if (!_tileShapes.ContainsKey((x, y)))
            return;

        var polygon = _tileShapes[(x, y)];
        polygon.Fill = _tiles[y][x] switch
        {
            '#' => Wall,
            'D' => Door,
            'K' => new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00)), // Gold for key
            _ => (x + y) % 2 == 0 ? Floor : FloorAlt
        };
    }

    private void PlacePlayer()
    {
        // Calculate isometric position (rotated 90 degrees clockwise)
        int isoX = (_py + _px) * (TileWidth / 2) + OffsetX;
        int isoY = (_py - _px) * (TileHeight / 2) + OffsetY;

        // Center player on the tile
        Canvas.SetLeft(_player, isoX + (TileWidth / 4) - (_player.Width / 2));
        Canvas.SetTop(_player, isoY + (TileHeight / 2) - (_player.Height / 2));

        // Render player above the floor tiles
        Panel.SetZIndex(_player, _px + _py + 1000);
    }

    // ---------- Input / movement ----------
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (GameScreen.Visibility != Visibility.Visible || _inQuiz) return;

        (int dx, int dy) = e.Key switch
        {
            Key.Up or Key.W => (1, 0),      // Up becomes Right
            Key.Down or Key.S => (-1, 0),   // Down becomes Left
            Key.Left or Key.A => (0, -1),   // Left becomes Up
            Key.Right or Key.D => (0, 1),   // Right becomes Down
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
        // Bounds checking - can't move outside the map
        if (nx < 0 || ny < 0 || ny >= _tiles.Length || nx >= _tiles[0].Length)
            return;

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