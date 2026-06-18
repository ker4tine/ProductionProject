using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace ProductionProject
{
    public class UserEditForm : Form
    {
        private readonly string connectionString;
        private readonly bool editing;
        private TextBox txtLogin;
        private TextBox txtPassword;
        private TextBox txtFullName;
        private ComboBox cmbRole;

        public string UserLogin { get; private set; }
        public string UserPassword { get; private set; }
        public string FullName { get; private set; }
        public int RoleId { get; private set; }

        public UserEditForm(string connectionString, string login = "", string fullName = "", int roleId = 0)
        {
            this.connectionString = connectionString;
            editing = !string.IsNullOrWhiteSpace(login);
            BuildForm();
            LoadRoles();

            txtLogin.Text = login;
            txtFullName.Text = fullName;
            if (roleId > 0) cmbRole.SelectedValue = roleId;
        }

        private void BuildForm()
        {
            Text = editing ? "Изменение пользователя" : "Добавление пользователя";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(380, 300);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Controls.Add(new Label { Text = "Логин:", Location = new Point(30, 30), AutoSize = true });
            txtLogin = new TextBox { Location = new Point(150, 27), Width = 180 };
            Controls.Add(txtLogin);

            Controls.Add(new Label { Text = editing ? "Новый пароль:" : "Пароль:", Location = new Point(30, 65), AutoSize = true });
            txtPassword = new TextBox { Location = new Point(150, 62), Width = 180, UseSystemPasswordChar = true };
            Controls.Add(txtPassword);

            Controls.Add(new Label { Text = "ФИО:", Location = new Point(30, 100), AutoSize = true });
            txtFullName = new TextBox { Location = new Point(150, 97), Width = 180 };
            Controls.Add(txtFullName);

            Controls.Add(new Label { Text = "Роль:", Location = new Point(30, 135), AutoSize = true });
            cmbRole = new ComboBox { Location = new Point(150, 132), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            Controls.Add(cmbRole);

            if (editing)
            {
                Controls.Add(new Label
                {
                    Text = "Оставьте пароль пустым, чтобы не менять его.",
                    Location = new Point(30, 170),
                    Size = new Size(300, 30),
                    ForeColor = Color.DimGray
                });
            }

            Button btnOk = new Button { Text = "Сохранить", Location = new Point(85, 215), Width = 100 };
            btnOk.Click += BtnOk_Click;
            Controls.Add(btnOk);
            Controls.Add(new Button { Text = "Отмена", Location = new Point(200, 215), Width = 100, DialogResult = DialogResult.Cancel });
        }

        private void LoadRoles()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand("SELECT role_id, role_name FROM Roles ORDER BY role_name", connection))
            {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        cmbRole.Items.Add(new RoleItem(
                            Convert.ToInt32(reader["role_id"]),
                            reader["role_name"].ToString()));
                    }
                }
            }

            if (cmbRole.Items.Count > 0) cmbRole.SelectedIndex = 0;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLogin.Text))
            {
                MessageBox.Show("Введите логин.", "Проверка данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!editing && string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Введите пароль.", "Проверка данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RoleItem role = cmbRole.SelectedItem as RoleItem;
            if (role == null)
            {
                MessageBox.Show("Выберите роль.", "Проверка данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            UserLogin = txtLogin.Text.Trim();
            UserPassword = txtPassword.Text;
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
