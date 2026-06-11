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
        private readonly List<PictureBox> targetCells = new List<PictureBox>();
        private readonly List<PictureBox> sourcePieces = new List<PictureBox>();
        private readonly Dictionary<PictureBox, int> placedPieces = new Dictionary<PictureBox, int>();
        private Image[] pieces;
        private int selectedPieceIndex = -1;
        private PictureBox selectedSourceBox;

        public CaptchaForm()
        {
            BuildForm();
        }

        private void BuildForm()
        {
            Text = "Проверка безопасности";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(720, 500);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Label lblInfo = new Label
            {
                Text = "Соберите картинку: выберите фрагмент снизу и нажмите на нужную ячейку сверху.",
                Location = new Point(25, 20),
                Size = new Size(650, 25)
            };
            Controls.Add(lblInfo);

            Image sourceImage = LoadRandomImage();
            pieces = SplitImage(sourceImage, 2, 2);

            Label lblTarget = new Label
            {
                Text = "Поле сборки:",
                Location = new Point(95, 55),
                Size = new Size(200, 25)
            };
            Controls.Add(lblTarget);

            Panel targetPanel = new Panel
            {
                Location = new Point(75, 80),
                Size = new Size(240, 240),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(targetPanel);

            int cellSize = 120;
            for (int i = 0; i < 4; i++)
            {
                PictureBox cell = new PictureBox
                {
                    Location = new Point((i % 2) * cellSize, (i / 2) * cellSize),
                    Size = new Size(cellSize, cellSize),
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.WhiteSmoke,
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Tag = i,
                    Cursor = Cursors.Hand
                };
                cell.Click += TargetCell_Click;
                targetCells.Add(cell);
                targetPanel.Controls.Add(cell);
            }

            Label lblSource = new Label
            {
                Text = "Фрагменты:",
                Location = new Point(420, 55),
                Size = new Size(200, 25)
            };
            Controls.Add(lblSource);

            Panel sourcePanel = new Panel
            {
                Location = new Point(385, 80),
                Size = new Size(250, 240),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(sourcePanel);

            List<int> order = new List<int> { 0, 1, 2, 3 };
            Shuffle(order);

            for (int i = 0; i < 4; i++)
            {
                int pieceIndex = order[i];
                PictureBox pieceBox = new PictureBox
                {
                    Location = new Point(10 + (i % 2) * 120, 10 + (i / 2) * 110),
                    Size = new Size(100, 100),
                    BorderStyle = BorderStyle.FixedSingle,
                    Image = pieces[pieceIndex],
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Tag = pieceIndex,
                    Cursor = Cursors.Hand
                };
                pieceBox.Click += SourcePiece_Click;
                sourcePieces.Add(pieceBox);
                sourcePanel.Controls.Add(pieceBox);
            }

            Button btnClear = new Button
            {
                Text = "Очистить",
                Location = new Point(185, 350),
                Width = 100
            };
            btnClear.Click += BtnClear_Click;

            Button btnOk = new Button
            {
                Text = "Проверить",
                Location = new Point(305, 350),
                Width = 110
            };
            btnOk.Click += BtnOk_Click;

            Button btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(435, 350),
                Width = 100,
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(btnClear);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
        }

        private Image LoadRandomImage()
        {
            int imageNumber = random.Next(1, 5);
            string imagePath = Path.Combine(Application.StartupPath, "Resources", "Captcha", imageNumber + ".png");

            if (File.Exists(imagePath))
            {
                using (Image img = Image.FromFile(imagePath))
                {
                    return new Bitmap(img, new Size(240, 240));
                }
            }

            Bitmap fallback = new Bitmap(240, 240);
            using (Graphics g = Graphics.FromImage(fallback))
            {
                g.Clear(Color.LightGray);
                g.DrawString("CAPTCHA", new Font("Arial", 24, FontStyle.Bold), Brushes.Black, 35, 95);
            }
            return fallback;
        }

        private Image[] SplitImage(Image image, int rows, int cols)
        {
            Image[] result = new Image[rows * cols];
            int width = image.Width / cols;
            int height = image.Height / rows;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    Bitmap piece = new Bitmap(width, height);
                    using (Graphics g = Graphics.FromImage(piece))
                    {
                        Rectangle dest = new Rectangle(0, 0, width, height);
                        Rectangle src = new Rectangle(col * width, row * height, width, height);
                        g.DrawImage(image, dest, src, GraphicsUnit.Pixel);
                    }
                    result[row * cols + col] = piece;
                }
            }

            return result;
        }

        private void SourcePiece_Click(object sender, EventArgs e)
        {
            if (selectedSourceBox != null)
            {
                selectedSourceBox.BackColor = SystemColors.Control;
            }

            selectedSourceBox = sender as PictureBox;
            selectedPieceIndex = Convert.ToInt32(selectedSourceBox.Tag);
            selectedSourceBox.BackColor = Color.LightBlue;
        }

        private void TargetCell_Click(object sender, EventArgs e)
        {
            if (selectedPieceIndex == -1)
            {
                MessageBox.Show("Сначала выберите фрагмент картинки.");
                return;
            }

            PictureBox targetCell = sender as PictureBox;

            targetCell.Image = pieces[selectedPieceIndex];
            placedPieces[targetCell] = selectedPieceIndex;

            selectedPieceIndex = -1;
            if (selectedSourceBox != null)
            {
                selectedSourceBox.BackColor = SystemColors.Control;
                selectedSourceBox = null;
            }
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            foreach (PictureBox cell in targetCells)
            {
                cell.Image = null;
            }

            placedPieces.Clear();
            selectedPieceIndex = -1;

            if (selectedSourceBox != null)
            {
                selectedSourceBox.BackColor = SystemColors.Control;
                selectedSourceBox = null;
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (placedPieces.Count < 4)
            {
                MessageBox.Show("Соберите картинку полностью.");
                return;
            }

            foreach (PictureBox cell in targetCells)
            {
                int correctIndex = Convert.ToInt32(cell.Tag);
                if (!placedPieces.ContainsKey(cell) || placedPieces[cell] != correctIndex)
                {
                    MessageBox.Show("Картинка собрана неверно.");
                    return;
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void Shuffle(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
