using System;
using System.Drawing;
using System.Windows.Forms;

namespace ProductionProject
{
    public class CaptchaForm : Form
    {
        private readonly int a;
        private readonly int b;
        private TextBox txtAnswer;

        public CaptchaForm()
        {
            Random random = new Random();
            a = random.Next(1, 10);
            b = random.Next(1, 10);
            BuildForm();
        }

        private void BuildForm()
        {
            Text = "Капча";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(300, 180);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Label lblQuestion = new Label
            {
                Text = "Введите результат: " + a + " + " + b,
                Location = new Point(35, 25),
                AutoSize = true
            };

            txtAnswer = new TextBox
            {
                Location = new Point(35, 55),
                Width = 200
            };

            Button btnOk = new Button
            {
                Text = "ОК",
                Location = new Point(35, 90),
                Width = 90
            };
            btnOk.Click += BtnOk_Click;

            Button btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(145, 90),
                Width = 90,
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(lblQuestion);
            Controls.Add(txtAnswer);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            int answer;
            if (int.TryParse(txtAnswer.Text.Trim(), out answer) && answer == a + b)
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
