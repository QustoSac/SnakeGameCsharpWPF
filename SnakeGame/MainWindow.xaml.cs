using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

namespace SnakeGame
{
    public partial class MainWindow : Window
    {
        private bool isPaused = false;
        private const int SnakeSquareSize = 20;
        private const int GameAreaWidth = 400;
        private const int GameAreaHeight = 400;
        private List<Rectangle> snake;
        private Point snakeDirection;
        private Point foodPosition;
        private Dictionary<string, int> playerScores;
        private string currentPlayerName;


        public MainWindow()
        {
            InitializeComponent();
            playerScores = LoadScores();
            UpdateTopPlayers();
        }

        private Rectangle CreateSnakeSegment(int x, int y)
        {
            var segment = new Rectangle
            {
                Width = SnakeSquareSize,
                Height = SnakeSquareSize,
                Fill = Brushes.Green
            };
            Canvas.SetLeft(segment, x * SnakeSquareSize);
            Canvas.SetTop(segment, y * SnakeSquareSize);
            GameCanvas.Children.Add(segment);
            return segment;
        }

        private Dictionary<string, int> LoadScores()
        {
            if (File.Exists("scores.json"))
            {
                string json = File.ReadAllText("scores.json");
                return JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
            }
            return new Dictionary<string, int>();
        }

        private void RestartGame()
        {
            var gameTimer = new DispatcherTimer();
            // Очистка поля перед началом новой игры
            foreach (var snakeSegment in snake)
            {
                GameCanvas.Children.Remove(snakeSegment);
            }
            snake.Clear();

            var food = GameCanvas.Children.OfType<Rectangle>().FirstOrDefault(x => x.Fill == Brushes.Red);
            if (food != null)
            {
                GameCanvas.Children.Remove(food); // Удаляем еду
            }

            // Сброс всех параметров игры
            snakeDirection = new Point(1, 0);
            for (int i = 0; i < 3; i++)
            {
                snake.Add(CreateSnakeSegment(5, 5));
            }
            SpawnFood(); // Создание новой еды
            count.Content = "Счет: 0";
            isPaused = false; // Сброс состояния паузы
            gameTimer.Interval = new TimeSpan(0, 0, 0, 0, 100); // Сброс скорости движения
        }

        private void GameOver()
        {
            MessageBoxResult result = MessageBox.Show("Игра окончена! Вы хотите перезапустить?", "Game Over", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                if (playerScores.ContainsKey(currentPlayerName))
                {
                    playerScores[currentPlayerName] = Math.Max(playerScores[currentPlayerName], snake.Count - 3);
                }
                else
                {
                    playerScores.Add(currentPlayerName, snake.Count - 3);
                }

                SaveScores();
                UpdateTopPlayers();
                RestartGame();
            }
            else
            {
                if (playerScores.ContainsKey(currentPlayerName))
                {
                    playerScores[currentPlayerName] = Math.Max(playerScores[currentPlayerName], snake.Count - 3);
                }
                else
                {
                    playerScores.Add(currentPlayerName, snake.Count - 3);
                }

                SaveScores();
                Application.Current.Shutdown();
            }
        }

        private void UpdateTopPlayers()
        {
            var topPlayers = playerScores.OrderByDescending(x => x.Value).Take(10);
            TopPlayersListBox.Items.Clear();
            foreach (var player in topPlayers)
            {
                TopPlayersListBox.Items.Add($"{player.Key}: {player.Value}");
            }
        }

        private void SaveScores()
        {
            string json = JsonConvert.SerializeObject(playerScores);
            File.WriteAllText("scores.json", json);
        }

        private void GameLoop(object sender, EventArgs e)
        {
            if (!isPaused)
            {
                MoveSnake();

                if (snake[0].TranslatePoint(new Point(0, 0), GameCanvas).Equals(foodPosition))
                {
                    snake.Add(CreateSnakeSegment((int)foodPosition.X, (int)foodPosition.Y));
                    GameCanvas.Children.Remove(GameCanvas.Children.OfType<Rectangle>().FirstOrDefault(x => x.Fill == Brushes.Red));
                    SpawnFood();
                }
            }
        }

        private void MoveSnake()
        {
            var head = snake[0];
            var newHeadPosition = new Point(Canvas.GetLeft(head) / SnakeSquareSize, Canvas.GetTop(head) / SnakeSquareSize);
            newHeadPosition.X += snakeDirection.X;
            newHeadPosition.Y += snakeDirection.Y;

            if (newHeadPosition.X < 0 || newHeadPosition.X >= GameAreaWidth / SnakeSquareSize ||
                newHeadPosition.Y < 0 || newHeadPosition.Y >= GameAreaHeight / SnakeSquareSize)
            {
                GameOver();
            }
            else
            {
                // Проверяем столкновение головы змеи с едой
                if (newHeadPosition.Equals(foodPosition))
                {
                    snake.Add(CreateSnakeSegment((int)foodPosition.X, (int)foodPosition.Y));
                    GameCanvas.Children.Remove(GameCanvas.Children.OfType<Rectangle>().FirstOrDefault(x => x.Fill == Brushes.Red)); // Удаляем съеденную еду
                    SpawnFood(); // Вызываем SpawnFood() для появления новой еды
                }

                count.Content = $"Счет: {snake.Count - 3}";

                // Проверяем столкновение головы змеи с ее телом
                for (int i = 1; i < snake.Count - 1; i++)
                {
                    var currentSegment = snake[i];
                    var currentSegmentPosition = new Point(Canvas.GetLeft(currentSegment) / SnakeSquareSize, Canvas.GetTop(currentSegment) / SnakeSquareSize);
                    if (newHeadPosition.Equals(currentSegmentPosition))
                    {
                        GameOver();
                        return;
                    }
                }

                Canvas.SetLeft(head, newHeadPosition.X * SnakeSquareSize);
                Canvas.SetTop(head, newHeadPosition.Y * SnakeSquareSize);

                // Перемещаем сегменты тела змеи
                for (int i = 1; i < snake.Count; i++)
                {
                    var nextPosition = new Point(Canvas.GetLeft(snake[i]) / SnakeSquareSize, Canvas.GetTop(snake[i]) / SnakeSquareSize);
                    Canvas.SetLeft(snake[i], newHeadPosition.X * SnakeSquareSize);
                    Canvas.SetTop(snake[i], newHeadPosition.Y * SnakeSquareSize);
                    newHeadPosition = nextPosition;
                }
            }
        }


        private void SpawnFood()
        {
            var random = new Random();
            int x = random.Next(0, GameAreaWidth / SnakeSquareSize);
            int y = random.Next(0, GameAreaHeight / SnakeSquareSize);

            bool onSnake = true;
            while (onSnake)
            {
                onSnake = false;
                foreach (var segment in snake)
                {
                    if (segment.TranslatePoint(new Point(0, 0), GameCanvas).Equals(new Point(x, y)))
                    {
                        onSnake = true;
                        x = random.Next(0, GameAreaWidth / SnakeSquareSize);
                        y = random.Next(0, GameAreaHeight / SnakeSquareSize);
                        break;
                    }
                }
            }


            var food = new Rectangle
            {
                Width = SnakeSquareSize,
                Height = SnakeSquareSize,
                Fill = Brushes.Red
            };
            Canvas.SetLeft(food, x * SnakeSquareSize);
            Canvas.SetTop(food, y * SnakeSquareSize);
            GameCanvas.Children.Add(food);

            foodPosition = new Point(x, y);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    snakeDirection = new Point(0, -1);
                    break;
                case Key.Down:
                    snakeDirection = new Point(0, 1);
                    break;
                case Key.Left:
                    snakeDirection = new Point(-1, 0);
                    break;
                case Key.Right:
                    snakeDirection = new Point(1, 0);
                    break;
                case Key.Space:
                    isPaused = !isPaused;
                    break;
            }
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            if (playerScores.ContainsKey(currentPlayerName))
            {
                playerScores[currentPlayerName] = Math.Max(playerScores[currentPlayerName], snake.Count - 3);
            }
            else
            {
                playerScores.Add(currentPlayerName, snake.Count - 3);
            }
            SaveScores();
            UpdateTopPlayers();

            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        private void StartGame_click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PlayerNameTextBox.Text))
            {
                MessageBox.Show("Введите ваше имя перед началом игры!");
                return;
            }

            PlayerNameTextBox.IsEnabled = false;
            AllPlayerList.IsEnabled = false;

            StartGame.Visibility = Visibility.Hidden;
            StartGame.Visibility = Visibility.Collapsed;

            GameCanvas.Background = new SolidColorBrush(Colors.WhiteSmoke); 

            snake = new List<Rectangle>();
            snakeDirection = new Point(1, 0);
            SpawnFood();


            for (int i = 0; i < 3; i++)
            {
                snake.Add(CreateSnakeSegment(10 - i, 10));
            }

            currentPlayerName = PlayerNameTextBox.Text;

            var gameTimer = new DispatcherTimer();
            gameTimer.Tick += GameLoop;
            gameTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            gameTimer.Start();
        }

        private void PlayerNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {            
            if (string.IsNullOrWhiteSpace(PlayerNameTextBox.Text))
            {
                StartGame.IsEnabled = false;
            }
            else
            {
                StartGame.IsEnabled = true;
            }        
        }


        private void OpenPlayerList(object sender, RoutedEventArgs e)
        {
            PlayerList playerList  = new PlayerList();
            playerList.Show();
        }
    }
}