using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MemoryCardGame
{
    public class GameForm : Form
    {
        private int difficulty; // 0=Easy, 1=Medium, 2=Hard
        private int rows;
        private int cols;

        private TableLayoutPanel gridLayout;
        private Timer gameTimer;
        private Label scoreLabel;

        private Button firstClicked = null;
        private Button secondClicked = null;
        private int matchCount = 0;
        private int totalPairs = 0;
        private int flipCount = 0;

        // 萬國碼圖案庫（.NET Framework 支援度極佳，不需安裝特別字型）
        private readonly List<string> allSymbols = new List<string>()
        {
            "★", "▲", "●", "■", "◆", "♥", "♣", "♠", "☀", "☁", "❄", "⚡", "⚓", "✈", "✿", "⚽", "🎵", "♨"
        };

        public GameForm(int diff)
        {
            this.difficulty = diff;
            ConfigureLayoutSettings();
            InitializeGameComponents();
        }

        private void ConfigureLayoutSettings()
        {
            // 依據傳入的難度決定行列與視窗大小
            if (difficulty == 0) // Easy
            {
                rows = 4; cols = 4;
                this.Size = new Size(500, 600);
                this.Text = "記憶翻牌 - 簡單 (4x4)";
            }
            else if (difficulty == 1) // Medium
            {
                rows = 4; cols = 6;
                this.Size = new Size(700, 600);
                this.Text = "記憶翻牌 - 中等 (4x6)";
            }
            else // Hard
            {
                rows = 6; cols = 6;
                this.Size = new Size(700, 780);
                this.Text = "記憶翻牌 - 困難 (6x6)";
            }

            totalPairs = (rows * cols) / 2;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 246, 250);
        }

        private void InitializeGameComponents()
        {
            // 上方狀態列
            Panel statusPanel = new Panel();
            statusPanel.Dock = DockStyle.Top;
            statusPanel.Height = 60;
            statusPanel.BackColor = Color.FromArgb(45, 52, 82);
            this.Controls.Add(statusPanel);

            // 分數顯示
            scoreLabel = new Label();
            scoreLabel.Text = $"配對進度: 0 / {totalPairs}  |  翻牌次數: 0";
            scoreLabel.Font = new Font("微軟正黑體", 12, FontStyle.Bold);
            scoreLabel.ForeColor = Color.White;
            scoreLabel.Dock = DockStyle.Fill;
            scoreLabel.TextAlign = ContentAlignment.MiddleCenter;
            statusPanel.Controls.Add(scoreLabel);

            // 返回選單按鈕
            Button btnBack = new Button();
            btnBack.Text = "返回選單";
            btnBack.Font = new Font("微軟正黑體", 10, FontStyle.Bold);
            btnBack.ForeColor = Color.White;
            btnBack.BackColor = Color.FromArgb(231, 76, 60);
            btnBack.FlatStyle = FlatStyle.Flat;
            btnBack.FlatAppearance.BorderSize = 0;
            btnBack.Size = new Size(100, 36);
            btnBack.Location = new Point(12, 12);
            btnBack.Click += (s, e) => this.Close();
            statusPanel.Controls.Add(btnBack);

            // 動態網格排版
            gridLayout = new TableLayoutPanel();
            gridLayout.Dock = DockStyle.Fill;
            gridLayout.Padding = new Padding(15);
            gridLayout.RowCount = rows;
            gridLayout.ColumnCount = cols;

            for (int i = 0; i < rows; i++)
                gridLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rows));
            for (int i = 0; i < cols; i++)
                gridLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / cols));

            this.Controls.Add(gridLayout);

            // 蓋牌計時器
            gameTimer = new Timer();
            gameTimer.Interval = 800;
            gameTimer.Tick += GameTimer_Tick;

            // 洗牌
            List<string> gameIcons = PrepareShuffledIcons();

            // 生成卡牌按鈕
            int iconIndex = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Button card = new Button();
                    card.Dock = DockStyle.Fill;
                    card.Margin = new Padding(6);
                    card.BackColor = Color.FromArgb(116, 125, 140);
                    card.FlatStyle = FlatStyle.Flat;
                    card.FlatAppearance.BorderSize = 0;
                    card.Cursor = Cursors.Hand;

                    card.Tag = gameIcons[iconIndex++]; // 答案藏在 Tag
                    card.Text = ""; // 封面空白
                    card.Font = new Font("Segoe UI Symbol", 22, FontStyle.Bold);

                    card.Click += Card_Click;
                    gridLayout.Controls.Add(card, c, r);
                }
            }
        }

        private List<string> PrepareShuffledIcons()
        {
            List<string> selected = new List<string>();
            for (int i = 0; i < totalPairs; i++)
            {
                selected.Add(allSymbols[i]);
                selected.Add(allSymbols[i]);
            }

            Random rand = new Random();
            int n = selected.Count;
            while (n > 1)
            {
                n--;
                int k = rand.Next(n + 1);
                string value = selected[k];
                selected[k] = selected[n];
                selected[n] = value;
            }
            return selected;
        }

        private void Card_Click(object sender, EventArgs e)
        {
            if (gameTimer.Enabled) return;

            Button clickedCard = sender as Button;
            if (clickedCard == null || clickedCard.Text != "") return;

            // 翻開牌
            clickedCard.Text = clickedCard.Tag.ToString();
            clickedCard.BackColor = Color.White;
            clickedCard.ForeColor = Color.FromArgb(47, 53, 66);

            if (firstClicked == null)
            {
                firstClicked = clickedCard;
                return;
            }

            secondClicked = clickedCard;
            flipCount++;
            UpdateStatusLabel();

            // 對答案
            if (firstClicked.Tag.ToString() == secondClicked.Tag.ToString())
            {
                // 答對了
                firstClicked.BackColor = Color.FromArgb(46, 204, 113);
                firstClicked.ForeColor = Color.White;
                secondClicked.BackColor = Color.FromArgb(46, 204, 113);
                secondClicked.ForeColor = Color.White;

                firstClicked = null;
                secondClicked = null;
                matchCount++;
                UpdateStatusLabel();

                if (matchCount == totalPairs)
                {
                    MessageBox.Show($"恭喜過關！\n總共翻牌次數：{flipCount} 次！", "挑戰成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
            }
            else
            {
                // 答錯，啟動計時器
                gameTimer.Start();
            }
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            gameTimer.Stop();

            // 蓋回去
            firstClicked.Text = "";
            firstClicked.BackColor = Color.FromArgb(116, 125, 140);
            secondClicked.Text = "";
            secondClicked.BackColor = Color.FromArgb(116, 125, 140);

            firstClicked = null;
            secondClicked = null;
        }

        private void UpdateStatusLabel()
        {
            scoreLabel.Text = $"配對進度: {matchCount} / {totalPairs}  |  翻牌次數: {flipCount}";
        }
    }
}