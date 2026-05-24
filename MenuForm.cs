using System;
using System.Drawing;
using System.Windows.Forms;

namespace MemoryCardGame
{
    // 這是一個純程式碼打造的視窗，不需要後台 Designer
    public class MenuForm : Form
    {
        public MenuForm()
        {
            this.Text = "記憶翻牌大考驗 - 選擇難度";
            this.Size = new Size(450, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(30, 30, 45);

            Label titleLabel = new Label();
            titleLabel.Text = "MEMORY GAME";
            titleLabel.Font = new Font("Segoe UI", 28, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(240, 240, 240);
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            titleLabel.Size = new Size(400, 60);
            titleLabel.Location = new Point(17, 40);
            this.Controls.Add(titleLabel);

            Label subTitleLabel = new Label();
            subTitleLabel.Text = "請選擇遊戲難度開始挑戰";
            subTitleLabel.Font = new Font("微軟正黑體", 12, FontStyle.Regular);
            subTitleLabel.ForeColor = Color.DarkGray;
            subTitleLabel.TextAlign = ContentAlignment.MiddleCenter;
            subTitleLabel.Size = new Size(400, 30);
            subTitleLabel.Location = new Point(17, 100);
            this.Controls.Add(subTitleLabel);

            Button btnEasy = CreateMenuButton("簡單 (4 x 4)", Color.FromArgb(46, 204, 113), 150);
            Button btnMedium = CreateMenuButton("中等 (4 x 6)", Color.FromArgb(241, 196, 15), 210);
            Button btnHard = CreateMenuButton("困難 (6 x 6)", Color.FromArgb(231, 76, 60), 270);

            btnEasy.Click += (s, e) => StartGame(0);
            btnMedium.Click += (s, e) => StartGame(1);
            btnHard.Click += (s, e) => StartGame(2);

            this.Controls.Add(btnEasy);
            this.Controls.Add(btnMedium);
            this.Controls.Add(btnHard);
        }

        private Button CreateMenuButton(string text, Color hoverColor, int yOffset)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Font = new Font("微軟正黑體", 14, FontStyle.Bold);
            btn.ForeColor = Color.White;
            btn.BackColor = Color.FromArgb(45, 45, 65);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = hoverColor;
            btn.Size = new Size(250, 45);
            btn.Location = new Point(92, yOffset);
            btn.Cursor = Cursors.Hand;
            return btn;
        }

        private void StartGame(int difficultyLevel)
        {
            this.Hide();
            using (GameForm gameForm = new GameForm(difficultyLevel))
            {
                gameForm.ShowDialog();
            }
            this.Show();
        }
    }
}