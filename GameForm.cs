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
        private Timer gameTimer;
        private object wmpPlayer;
        private Label scoreLabel;

        private Button firstClicked = null;
        private Button secondClicked = null;
        private int matchCount = 0;
        private int totalPairs = 0;
        private int flipCount = 0;
 

        // ★關鍵修改 1：移除 Unicode 符號，改用 Image 型態的清單★
        private List<Image> allImages = new List<Image>();

        public GameForm(int diff)
        {
            this.difficulty = diff;
            LoadGameImages(); // 載入資源圖片
            ConfigureLayoutSettings();
            InitializeGameComponents();

            try
            {
                // 1. 在電腦暫存區規劃一個位置放 MP3
                string tempMp3Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "start.mp3");

                // 2. 把資源檔的 byte[] 寫成暫存檔
                if (!System.IO.File.Exists(tempMp3Path))
                {
                    System.IO.File.WriteAllBytes(tempMp3Path, Properties.Resources.start);
                }

                // 3. 呼叫 WMP 元件播放這個暫存的 MP3 檔
                Type wmpType = Type.GetTypeFromProgID("WMPlayer.OCX.7");
                if (wmpType != null)
                {
                    wmpPlayer = Activator.CreateInstance(wmpType);
                    wmpType.InvokeMember("URL", System.Reflection.BindingFlags.SetProperty, null, wmpPlayer, new object[] { tempMp3Path });
                }
            }
            catch { /* 防呆 */ }
        }

        /// <summary>
        /// ★關鍵修改 2：從資源檔載入圖片★
        /// 這裡假設你在資源檔加入了 img1~img18 以及 cardBack
        /// </summary>
        private void LoadGameImages()
        {
            try
            {
                // 從 Properties.Resources 讀取圖片實體
                // 注意：這裡的名字必須跟你在 Resources.resx 裡的名字完全一致
                allImages.Add(Properties.Resources.pic1);
                allImages.Add(Properties.Resources.pic2);
                allImages.Add(Properties.Resources.pic3);
                allImages.Add(Properties.Resources.pic4);
                allImages.Add(Properties.Resources.pic5);
                allImages.Add(Properties.Resources.pic6);
                allImages.Add(Properties.Resources.pic7);
                allImages.Add(Properties.Resources.pic8);

                // 如果是困難模式，可能需要更多圖
                if (difficulty >= 1) // Medium 以上
                {
                    allImages.Add(Properties.Resources.pic9);
                    allImages.Add(Properties.Resources.pic10);
                    allImages.Add(Properties.Resources.pic11);
                    allImages.Add(Properties.Resources.pic12);
                }
                if (difficulty >= 2) // Hard
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
                this.Size = new Size(600, 700); // 圖片通常較大，視窗調大一點
                this.Text = "記憶翻牌 - 簡單 (4x4)";
            }
            else if (difficulty == 1) // Medium
            {
                rows = 4; cols = 6;
                this.Size = new Size(900, 700);
                this.Text = "記憶翻牌 - 中等 (4x6)";
            }
            else // Hard
            {
                rows = 6; cols = 6;
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
            // 上方狀態列 (維持原樣)
            Panel statusPanel = new Panel();
            statusPanel.Dock = DockStyle.Top;
            statusPanel.Height = 60;
            statusPanel.BackColor = Color.FromArgb(45, 52, 82);
            this.Controls.Add(statusPanel);

            scoreLabel = new Label();
            scoreLabel.Text = $"配對進度: 0 / {totalPairs}  |  翻牌次數: 0";
            scoreLabel.Font = new Font("微軟正黑體", 12, FontStyle.Bold);
            scoreLabel.ForeColor = Color.White;
            scoreLabel.Dock = DockStyle.Fill;
            scoreLabel.TextAlign = ContentAlignment.MiddleCenter;
            statusPanel.Controls.Add(scoreLabel);

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

            // 動態網格排版 (維持原樣)
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
            gameTimer = new Timer();
            gameTimer.Interval = 800; // 錯牌顯示時間
            gameTimer.Tick += GameTimer_Tick;

            // ★關鍵修改 3：移除字型設定，改用圖片洗牌★
            List<Image> gameImages = PrepareShuffledImages();

            int imgIndex = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Button card = new Button();
                    card.Dock = DockStyle.Fill;
                    card.Margin = new Padding(6);
                    card.BackColor = Color.White; // 圖片底色通常設為白色
                    card.FlatStyle = FlatStyle.Flat;
                    card.FlatAppearance.BorderSize = 1; // 給個細邊框
                    card.FlatAppearance.BorderColor = Color.LightGray;
                    card.Cursor = Cursors.Hand;

                    // ★關鍵修改 4：設定圖片屬性★
                    card.Tag = gameImages[imgIndex++]; // 真正的圖片藏在 Tag (object型態)
                    card.Image = Properties.Resources.back; // 預設顯示背面圖片
                    card.ImageAlign = ContentAlignment.MiddleCenter; // 圖片居中

                    card.Click += Card_Click;
                    gridLayout.Controls.Add(card, c, r);
                }
            }
            statusPanel.BringToFront(); // 強制把狀態列拉到最前層
            gridLayout.Padding = new Padding(15, 75, 15, 15); // 精準控制：上內距加大到 75，直接把牌往下推！
        }

        /// <summary>
        /// ★關鍵修改 5：將 Image 型態的圖片進行洗牌★
        /// </summary>
        private List<Image> PrepareShuffledImages()
        {
            List<Image> selected = new List<Image>();
            for (int i = 0; i < totalPairs; i++)
            {
                // 每個圖片加兩次
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

        private void Card_Click(object sender, EventArgs e)
        {
            if (gameTimer.Enabled) return;

            Button clickedCard = sender as Button;
            if (clickedCard == null) return;

            // 🔥 修改這裡：如果這張牌就是已經被點的第一張牌，或是已經配對成功（邊框變綠色）的牌，就不能再點
            if (clickedCard == firstClicked || clickedCard.FlatAppearance.BorderSize == 3) return;

            // 翻開牌：將 Image 屬性設為 Tag 裡藏的正面圖片
            clickedCard.Image = (Image)clickedCard.Tag;

            if (firstClicked == null)
            {
                firstClicked = clickedCard;
                return;
            }

            secondClicked = clickedCard;
            flipCount++;
            UpdateStatusLabel();

            // 對答案：直接比對兩個 Image 物件的參照是否相同
            if (firstClicked.Tag == secondClicked.Tag)
            {
                // 答對了 (维持翻開，不可再點擊)
                firstClicked.FlatAppearance.BorderColor = Color.FromArgb(46, 204, 113); // 成功邊框變綠色
                firstClicked.FlatAppearance.BorderSize = 3;
                secondClicked.FlatAppearance.BorderColor = Color.FromArgb(46, 204, 113);
                secondClicked.FlatAppearance.BorderSize = 3;

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

            // ★關鍵修改 8：錯牌蓋回去★
            // 將 Image 屬性恢復為背面圖片
            firstClicked.Image = Properties.Resources.back;
            secondClicked.Image = Properties.Resources.back;

            firstClicked = null;
            secondClicked = null;
        }

        private void UpdateStatusLabel()
        {
            scoreLabel.Text = $"配對進度: {matchCount} / {totalPairs}  |  翻牌次數: {flipCount}";
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
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