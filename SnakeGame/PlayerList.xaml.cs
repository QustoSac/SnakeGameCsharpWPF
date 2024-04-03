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
    public partial class PlayerList : Window
    {
        private Dictionary<string, int> playerScores;

        public PlayerList()
        {
            InitializeComponent();
            playerScores = LoadScores();
            var topPlayers = playerScores.OrderByDescending(x => x.Value);
            foreach (var player in topPlayers)
            {
                PlayerListBox.Items.Add($"{player.Key}: {player.Value}");
            }
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
    }
}
