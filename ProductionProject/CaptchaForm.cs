using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ProductionProject
{
    public class CaptchaForm : Form
    {
        private readonly int correctImageNumber;
        private int selectedImageNumber = 0;
        private readonly List<Button> optionButtons = new List<Button>();

        public CaptchaForm()
        {
            Random random = new Random();
            correctImageNumber = random.Next(1, 5);
            BuildForm();
        }

        private void BuildForm()
        {
            Text = "Проверка безопасности";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(620, 430);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Label lblInfo = new Label
            {
                Text = "Выберите такую же картинку, как в образце сверху.",
                Location = new Point(25, 20),
                Size = new Size(540, 25)
            };

            PictureBox samplePicture = new PictureBox
            {
                Location = new Point(210, 50),
                Size = new Size(180, 110),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            LoadImage(samplePicture, correctImageNumber);

            Label lblChoose = new Label
            {
                Text = "Варианты ответа:",
                Location = new Point(25, 180),
                Size = new Size(200, 25)
            };

            Controls.Add(lblInfo);
            Controls.Add(samplePicture);
            Controls.Add(lblChoose);

            for (int i = 1; i <= 4; i++)
            {
                int imageNumber = i;
                Panel panel = new Panel
                {
                    Location = new Point(25 + (i - 1) * 145, 210),
                    Size = new Size(130, 120),
                    BorderStyle = BorderStyle.FixedSingle
                };

                PictureBox optionPicture = new PictureBox
                {
                    Location = new Point(5, 5),
                    Size = new Size(118, 75),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Cursor = Cursors.Hand
                };
                LoadImage(optionPicture, imageNumber);
                optionPicture.Click += (s, e) => SelectImage(imageNumber);

                Button optionButton = new Button
                {
                    Text = "Выбрать",
                    Location = new Point(15, 86),
                    Size = new Size(95, 25),
                    Tag = imageNumber
                };
                optionButton.Click += (s, e) => SelectImage(imageNumber);
                optionButtons.Add(optionButton);

                panel.Controls.Add(optionPicture);
                panel.Controls.Add(optionButton);
                Controls.Add(panel);
            }

            Button btnOk = new Button
            {
                Text = "Подтвердить",
                Location = new Point(190, 350),
                Width = 110
            };
            btnOk.Click += BtnOk_Click;

            Button btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(320, 350),
                Width = 110,
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(btnOk);
            Controls.Add(btnCancel);
        }

        private void LoadImage(PictureBox pictureBox, int imageNumber)
        {
            string imagePath = Path.Combine(Application.StartupPath, "Resources", "Captcha", imageNumber + ".png");

            if (File.Exists(imagePath))
            {
                pictureBox.Image = Image.FromFile(imagePath);
            }
            else
            {
                pictureBox.BackColor = Color.LightGray;
            }
        }

        private void SelectImage(int imageNumber)
        {
            selectedImageNumber = imageNumber;

            foreach (Button button in optionButtons)
            {
                int buttonImageNumber = Convert.ToInt32(button.Tag);
                button.Text = buttonImageNumber == selectedImageNumber ? "Выбрано" : "Выбрать";
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (selectedImageNumber == 0)
            {
                MessageBox.Show("Сначала выберите картинку.");
                return;
            }

            if (selectedImageNumber == correctImageNumber)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show("Выбрана неверная картинка.");
            }
        }
    }
}
