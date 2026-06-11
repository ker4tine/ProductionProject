using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ProductionProject
{
    public class CaptchaForm : Form
    {
        private readonly string correctAnswer;
        private TextBox txtAnswer;
        private PictureBox pictureBox;

        public CaptchaForm()
        {
            Random random = new Random();
            int imageNumber = random.Next(1, 5);
            correctAnswer = imageNumber.ToString();
            BuildForm(imageNumber);
        }

        private void BuildForm(int imageNumber)
        {
            Text = "Капча";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(420, 330);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Label lblInfo = new Label
            {
                Text = "Введите номер картинки, которая показана на экране.",
                Location = new Point(25, 20),
                Size = new Size(360, 25)
            };

            pictureBox = new PictureBox
            {
                Location = new Point(25, 55),
                Size = new Size(350, 150),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            string imagePath = Path.Combine(Application.StartupPath, "Resources", "Captcha", imageNumber + ".png");
            if (File.Exists(imagePath))
            {
                pictureBox.Image = Image.FromFile(imagePath);
            }
            else
            {
                Label lblError = new Label
                {
                    Text = "Файл капчи не найден: " + imagePath,
                    Location = new Point(25, 210),
                    Size = new Size(360, 35),
                    ForeColor = Color.Red
                };
                Controls.Add(lblError);
            }

            Label lblAnswer = new Label
            {
                Text = "Ответ:",
                Location = new Point(25, 220),
                AutoSize = true
            };

            txtAnswer = new TextBox
            {
                Location = new Point(90, 217),
                Width = 120
            };

            Button btnOk = new Button
            {
                Text = "ОК",
                Location = new Point(90, 255),
                Width = 90
            };
            btnOk.Click += BtnOk_Click;

            Button btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(200, 255),
                Width = 90,
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(lblInfo);
            Controls.Add(pictureBox);
            Controls.Add(lblAnswer);
            Controls.Add(txtAnswer);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (txtAnswer.Text.Trim() == correctAnswer)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show("Неверный ответ.");
            }
        }
    }
}
