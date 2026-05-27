using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace MemoryCardGame
{
    public class GameForm : Form
    {
        private int difficulty;
        private int rows;
        private int cols;

        private TableLayoutPanel gridLayout;
        private Timer gameTimer; // 用來處理翻錯牌延遲蓋回的 Timer
        private object wmpPlayer; // 專屬 MP3 播放器
        private Label scoreLabel;

        private Button firstClicked = null;
        private Button secondClicked = null;
        private int matchCount = 0;
        private int totalPairs = 0;
        private int flipCount = 0;

        // 👑 【新功能：倒數計時與分數變數】
        private Timer countdownTimer; // 負責倒數計時的全新 Timer
        private int timeLeft = 60;    // 剩餘時間（秒）
        private int playerScore = 0;  // 玩家目前的總得分
        private int comboCount = 0;   // 目前的連續配對成功次數 (Combo)

        private List<Image> allImages = new List<Image>();

        public GameForm(int diff)
        {
            this.difficulty = diff;
            LoadGameImages();
            ConfigureLayoutSettings();
            InitializeGameComponents();

            // 🎵 開場 MP3 專屬播放區
            try
            {
                string tempMp3Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "memory_game_start.mp3");
                if (!System.IO.File.Exists(tempMp3Path))
                {
                    System.IO.File.WriteAllBytes(tempMp3Path, Properties.Resources.start);
                }

                Type wmpType = Type.GetTypeFromProgID("WMPlayer.OCX.7");
                if (wmpType != null)
                {
                    wmpPlayer = Activator.CreateInstance(wmpType);
                    wmpType.InvokeMember("URL", System.Reflection.BindingFlags.SetProperty, null, wmpPlayer, new object[] { tempMp3Path });
                }
            }
            catch { /* 防呆 */ }
        }

        private void LoadGameImages()
        {
            try
            {
                allImages.Add(Properties.Resources.pic1);
                allImages.Add(Properties.Resources.pic2);
                allImages.Add(Properties.Resources.pic3);
                allImages.Add(Properties.Resources.pic4);
                allImages.Add(Properties.Resources.pic5);
                allImages.Add(Properties.Resources.pic6);
                allImages.Add(Properties.Resources.pic7);
                allImages.Add(Properties.Resources.pic8);

                if (difficulty >= 1)
                {
                    allImages.Add(Properties.Resources.pic9);
                    allImages.Add(Properties.Resources.pic10);
                    allImages.Add(Properties.Resources.pic11);
                    allImages.Add(Properties.Resources.pic12);
                }
                if (difficulty >= 2)
                {
                    allImages.Add(Properties.Resources.pic13);
                    allImages.Add(Properties.Resources.pic14);
                    allImages.Add(Properties.Resources.pic15);
                    allImages.Add(Properties.Resources.pic16);
                    allImages.Add(Properties.Resources.pic17);
                    allImages.Add(Properties.Resources.pic18);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"圖片載入失敗，請確認資源檔設定。\n錯誤訊息: {ex.Message}", "錯誤");
                this.Close();
            }
        }

        private void ConfigureLayoutSettings()
        {
            if (difficulty == 0) // Easy
            {
                rows = 4; cols = 4;
                timeLeft = 60; // 簡單模式給 60 秒
                this.Size = new Size(600, 700);
                this.Text = "記憶翻牌 - 簡單 (4x4)";
            }
            else if (difficulty == 1) // Medium
            {
                rows = 4; cols = 6;
                timeLeft = 90; // 中等模式給 90 秒
                this.Size = new Size(900, 700);
                this.Text = "記憶翻牌 - 中等 (4x6)";
            }
            else // Hard
            {
                rows = 6; cols = 6;
                timeLeft = 120; // 困難模式給 120 秒
                this.Size = new Size(900, 1000);
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
            Panel statusPanel = new Panel();
            statusPanel.Dock = DockStyle.Top;
            statusPanel.Height = 60;
            statusPanel.BackColor = Color.FromArgb(45, 52, 82);
            this.Controls.Add(statusPanel);

            scoreLabel = new Label();
            scoreLabel.Font = new Font("微軟正黑體", 12, FontStyle.Bold);
            scoreLabel.ForeColor = Color.White;
            scoreLabel.Dock = DockStyle.Fill;
            scoreLabel.TextAlign = ContentAlignment.MiddleCenter;
            statusPanel.Controls.Add(scoreLabel);
            UpdateStatusLabel(); // 初始化狀態列文字

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

            // 處理翻錯牌的 Timer
            gameTimer = new Timer();
            gameTimer.Interval = 800;
            gameTimer.Tick += GameTimer_Tick;

            // 👑 【新功能：啟動倒數計時的 Timer】
            countdownTimer = new Timer();
            countdownTimer.Interval = 1000; // 每 1000 毫秒 (1秒) 觸發一次
            countdownTimer.Tick += CountdownTimer_Tick;
            countdownTimer.Start(); // 遊戲開始，啟動倒數！

            List<Image> gameImages = PrepareShuffledImages();

            int imgIndex = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Button card = new Button();
                    card.Dock = DockStyle.Fill;
                    card.Margin = new Padding(6);
                    card.BackColor = Color.White;
                    card.FlatStyle = FlatStyle.Flat;
                    card.FlatAppearance.BorderSize = 1;
                    card.FlatAppearance.BorderColor = Color.LightGray;
                    card.Cursor = Cursors.Hand;

                    card.Tag = gameImages[imgIndex++];
                    card.Image = Properties.Resources.back;
                    card.ImageAlign = ContentAlignment.MiddleCenter;

                    card.Click += Card_Click;
                    gridLayout.Controls.Add(card, c, r);
                }
            }
            statusPanel.BringToFront();
            gridLayout.Padding = new Padding(15, 75, 15, 15);
        }

        private List<Image> PrepareShuffledImages()
        {
            List<Image> selected = new List<Image>();
            for (int i = 0; i < totalPairs; i++)
            {
                selected.Add(allImages[i]);
                selected.Add(allImages[i]);
            }

            Random rand = new Random();
            int n = selected.Count;
            while (n > 1)
            {
                n--;
                int k = rand.Next(n + 1);
                Image value = selected[k];
                selected[k] = selected[n];
                selected[n] = value;
            }
            return selected;
        }

        // 👑 【新功能：每秒執行一次的倒數計時事件】
        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            timeLeft--; // 時間減 1 秒
            UpdateStatusLabel();

            if (timeLeft <= 0)
            {
                countdownTimer.Stop(); // 停止倒數
                MessageBox.Show("時間到！挑戰失敗了，再試一次吧！", "遊戲結束", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Close(); // 關閉遊戲視窗，退回選單
            }
        }

        private void Card_Click(object sender, EventArgs e)
        {
            if (gameTimer.Enabled || timeLeft <= 0) return;

            Button clickedCard = sender as Button;
            if (clickedCard == null) return;

            if (clickedCard == firstClicked || clickedCard.FlatAppearance.BorderSize == 3) return;

            clickedCard.Image = (Image)clickedCard.Tag;

            if (firstClicked == null)
            {
                firstClicked = clickedCard;
                return;
            }

            secondClicked = clickedCard;
            flipCount++;

            // 對答案
            if (firstClicked.Tag == secondClicked.Tag)
            {
                // 👑 【新功能：配對成功，加分並累積 Combo！】
                comboCount++; // 連擊數 +1

                // 基礎分 100 分，根據 Combo 給予加成（例：Combo x2 拿 200 分，Combo x3 拿 300 分）
                int scoreGained = 100 * comboCount;
                playerScore += scoreGained;

                firstClicked.FlatAppearance.BorderColor = Color.FromArgb(46, 204, 113);
                firstClicked.FlatAppearance.BorderSize = 3;
                secondClicked.FlatAppearance.BorderColor = Color.FromArgb(46, 204, 113);
                secondClicked.FlatAppearance.BorderSize = 3;

                firstClicked = null;
                secondClicked = null;
                matchCount++;
                UpdateStatusLabel();

                if (matchCount == totalPairs)
                {
                    countdownTimer.Stop(); // 贏了就要立刻停止計時！
                    MessageBox.Show($"恭喜過關！\n總完成時間：{this.Text}\n總翻牌次數：{flipCount} 次\n最終總得分：{playerScore} 分！", "挑戰成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
            }
            else
            {
                // 👑 【新功能：配對失敗，Combo 立刻斷掉歸零！】
                comboCount = 0;
                UpdateStatusLabel();

                gameTimer.Start(); // 啟動蓋牌計時器
            }
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            gameTimer.Stop();

            firstClicked.Image = Properties.Resources.back;
            secondClicked.Image = Properties.Resources.back;

            firstClicked = null;
            secondClicked = null;
        }

        // 👑 【新功能：狀態列同步更新時間、分數與 Combo 狀態】
        private void UpdateStatusLabel()
        {
            string comboText = comboCount > 1 ? $"  🔥 Combo x{comboCount}!" : "";
            scoreLabel.Text = $"⏳ 剩餘時間: {timeLeft} 秒  |  🎯 分數: {playerScore} 分{comboText}\n[ 進度: {matchCount} / {totalPairs}  |  翻牌次數: {flipCount} ]";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // 安全退場：關閉視窗時要把兩個計時器都關掉
            if (countdownTimer != null) countdownTimer.Stop();
            if (gameTimer != null) gameTimer.Stop();

            if (wmpPlayer != null)
            {
                try
                {
                    wmpPlayer.GetType().InvokeMember("close", System.Reflection.BindingFlags.InvokeMethod, null, wmpPlayer, null);
                }
                catch { }
            }
        }
    }
}