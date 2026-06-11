using System;
using System.Drawing;
using System.Windows.Forms;

namespace ProductionProject
{
    public class NoteEditForm : Form
    {
        private TextBox txtTitle;
        private TextBox txtContent;

        public string NoteTitle { get; private set; }
        public string NoteContent { get; private set; }

        public NoteEditForm(string title, string noteTitle = "", string noteContent = "")
        {
            NoteTitle = noteTitle;
            NoteContent = noteContent;
            BuildForm(title);
        }

        private void BuildForm(string title)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(520, 380);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Controls.Add(new Label { Text = "Заголовок:", Location = new Point(30, 30), AutoSize = true });
            txtTitle = new TextBox { Location = new Point(120, 27), Width = 340, Text = NoteTitle };
            Controls.Add(txtTitle);

            Controls.Add(new Label { Text = "Содержание:", Location = new Point(30, 70), AutoSize = true });
            txtContent = new TextBox
            {
                Location = new Point(120, 67),
                Width = 340,
                Height = 190,
                Text = NoteContent,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            Controls.Add(txtContent);

            Button btnOk = new Button { Text = "Сохранить", Location = new Point(150, 290), Width = 100 };
            btnOk.Click += BtnOk_Click;
            Button btnCancel = new Button { Text = "Отмена", Location = new Point(270, 290), Width = 100, DialogResult = DialogResult.Cancel };
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageBox.Show("Введите заголовок.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtContent.Text))
            {
                MessageBox.Show("Введите содержание заметки.");
                return;
            }

            NoteTitle = txtTitle.Text.Trim();
            NoteContent = txtContent.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
