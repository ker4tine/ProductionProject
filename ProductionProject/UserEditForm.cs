using System;
using System.Drawing;
using System.Windows.Forms;

namespace ProductionProject
{
    public class UserEditForm : Form
    {
        private TextBox txtLogin;
        private TextBox txtPassword;
        private TextBox txtFullName;
        private ComboBox cmbRole;

        public string UserLogin { get; private set; }
        public string UserPassword { get; private set; }
        public string FullName { get; private set; }
        public int RoleId { get; private set; }

        public UserEditForm()
        {
            BuildForm();
        }

        private void BuildForm()
        {
            Text = "Добавление пользователя";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(360, 280);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Label lblLogin = new Label { Text = "Логин:", Location = new Point(30, 30), AutoSize = true };
            txtLogin = new TextBox { Location = new Point(140, 27), Width = 160 };

            Label lblPassword = new Label { Text = "Пароль:", Location = new Point(30, 65), AutoSize = true };
            txtPassword = new TextBox { Location = new Point(140, 62), Width = 160 };

            Label lblFullName = new Label { Text = "ФИО:", Location = new Point(30, 100), AutoSize = true };
            txtFullName = new TextBox { Location = new Point(140, 97), Width = 160 };

            Label lblRole = new Label { Text = "Роль:", Location = new Point(30, 135), AutoSize = true };
            cmbRole = new ComboBox { Location = new Point(140, 132), Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRole.Items.Add(new RoleItem(1, "Администратор"));
            cmbRole.Items.Add(new RoleItem(2, "Пользователь"));
            cmbRole.SelectedIndex = 1;

            Button btnOk = new Button { Text = "Сохранить", Location = new Point(75, 185), Width = 100 };
            btnOk.Click += BtnOk_Click;

            Button btnCancel = new Button { Text = "Отмена", Location = new Point(190, 185), Width = 100, DialogResult = DialogResult.Cancel };

            Controls.Add(lblLogin);
            Controls.Add(txtLogin);
            Controls.Add(lblPassword);
            Controls.Add(txtPassword);
            Controls.Add(lblFullName);
            Controls.Add(txtFullName);
            Controls.Add(lblRole);
            Controls.Add(cmbRole);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLogin.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Введите логин и пароль.");
                return;
            }

            RoleItem role = cmbRole.SelectedItem as RoleItem;
            UserLogin = txtLogin.Text.Trim();
            UserPassword = txtPassword.Text.Trim();
            FullName = txtFullName.Text.Trim();
            RoleId = role.Id;
            DialogResult = DialogResult.OK;
            Close();
        }

        private class RoleItem
        {
            public int Id { get; private set; }
            public string Name { get; private set; }

            public RoleItem(int id, string name)
            {
                Id = id;
                Name = name;
            }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}
