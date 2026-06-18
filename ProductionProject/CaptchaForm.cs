using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ProductionProject
{
    public class CaptchaForm : Form
    {
        private readonly Random random = new Random();
        private readonly List<PictureBox> boxes = new List<PictureBox>();
        private readonly Dictionary<PictureBox, int> angles = new Dictionary<PictureBox, int>();
        private readonly int maxAttempts;
        private Image[] parts;

        public int FailedAttempts { get; private set; }

        public CaptchaForm(int maxAttempts = 3)
        {
            this.maxAttempts = Math.Max(1, maxAttempts);
            BuildForm();
        }

        private void BuildForm()
        {
            Text = "Проверка безопасности";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(420, 460);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Controls.Add(new Label
            {
                Text = "Нажимайте на части картинки, чтобы повернуть их правильно.",
                Location = new Point(25, 20),
                Size = new Size(360, 40),
                TextAlign = ContentAlignment.MiddleCenter
            });

            parts = SplitImage(LoadImage(), 2, 2);

            Panel panel = new Panel
            {
                Location = new Point(80, 80),
                Size = new Size(240, 240),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(panel);

            for (int i = 0; i < 4; i++)
            {
                PictureBox box = new PictureBox
                {
                    Location = new Point((i % 2) * 120, (i / 2) * 120),
                    Size = new Size(120, 120),
                    BorderStyle = BorderStyle.FixedSingle,
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Cursor = Cursors.Hand,
                    Tag = i
                };

                int angle = RandomAngle();
                angles[box] = angle;
                box.Image = Rotate(parts[i], angle);
                box.Click += Box_Click;
                boxes.Add(box);
                panel.Controls.Add(box);
            }

            if (Solved()) MakeNotSolved();

            Button btnRefresh = new Button { Text = "Обновить", Location = new Point(65, 350), Width = 100 };
            btnRefresh.Click += BtnRefresh_Click;
            Controls.Add(btnRefresh);

            Button btnOk = new Button { Text = "Проверить", Location = new Point(175, 350), Width = 100 };
            btnOk.Click += BtnOk_Click;
            Controls.Add(btnOk);

            Controls.Add(new Button
            {
                Text = "Отмена",
                Location = new Point(285, 350),
                Width = 80,
                DialogResult = DialogResult.Cancel
            });
        }

        private Image LoadImage()
        {
            string path = Path.Combine(Application.StartupPath, "Resources", "Captcha", "1.png");
            if (File.Exists(path))
            {
                using (Image img = Image.FromFile(path))
                    return new Bitmap(img, new Size(240, 240));
            }

            Bitmap bmp = new Bitmap(240, 240);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.LightGray);
                g.DrawString("CAPTCHA", new Font("Arial", 24, FontStyle.Bold), Brushes.Black, 35, 95);
            }
            return bmp;
        }

        private Image[] SplitImage(Image image, int rows, int cols)
        {
            Image[] result = new Image[rows * cols];
            int w = image.Width / cols;
            int h = image.Height / rows;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    Bitmap piece = new Bitmap(w, h);
                    using (Graphics g = Graphics.FromImage(piece))
                    {
                        g.DrawImage(image, new Rectangle(0, 0, w, h), new Rectangle(col * w, row * h, w, h), GraphicsUnit.Pixel);
                    }
                    result[row * cols + col] = piece;
                }
            }
            return result;
        }

        private void Box_Click(object sender, EventArgs e)
        {
            PictureBox box = sender as PictureBox;
            if (box == null) return;

            int index = Convert.ToInt32(box.Tag);
            angles[box] = (angles[box] + 90) % 360;
            box.Image = Rotate(parts[index], angles[box]);
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            foreach (PictureBox box in boxes)
            {
                int index = Convert.ToInt32(box.Tag);
                int angle = RandomAngle();
                angles[box] = angle;
                box.Image = Rotate(parts[index], angle);
            }
            if (Solved()) MakeNotSolved();
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (Solved())
            {
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            FailedAttempts++;
            int attemptsLeft = maxAttempts - FailedAttempts;
            if (attemptsLeft <= 0)
            {
                MessageBox.Show(
                    "Превышено допустимое число ошибок. Учетная запись будет заблокирована.",
                    "Проверка не пройдена",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                DialogResult = DialogResult.Abort;
                Close();
                return;
            }

            MessageBox.Show(
                "Картинка собрана неверно. Осталось попыток: " + attemptsLeft + ".",
                "Проверка не пройдена",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private bool Solved()
        {
            foreach (int angle in angles.Values)
                if (angle != 0) return false;
            return true;
        }

        private int RandomAngle()
        {
            int[] values = { 0, 90, 180, 270 };
            return values[random.Next(values.Length)];
        }

        private void MakeNotSolved()
        {
            if (boxes.Count == 0) return;
            PictureBox first = boxes[0];
            angles[first] = 90;
            first.Image = Rotate(parts[0], 90);
        }

        private Image Rotate(Image image, int angle)
        {
            Bitmap bmp = new Bitmap(image);
            if (angle == 90) bmp.RotateFlip(RotateFlipType.Rotate90FlipNone);
            else if (angle == 180) bmp.RotateFlip(RotateFlipType.Rotate180FlipNone);
            else if (angle == 270) bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
            return bmp;
        }
    }
}
