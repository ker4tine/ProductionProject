using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace ProductionProject
{
    public partial class Form1 : Form
    {
        private readonly string connectionString = @"Data Source=WIN-BBJB9MMFFR1\SQLEXPRESS;Initial Catalog=PracticeDB;Integrated Security=True;TrustServerCertificate=True";

        private TextBox txtLogin;
        private TextBox txtPassword;
        private Button btnLogin;
        private Label lblMessage;

        private int failedAttempts = 0;
        private CurrentUserData currentUser;
        private NotesApiServer apiServer;

        private class CurrentUserData
        {
            public int Id;
            public string Login;
            public string Role;
        }

        public Form1()
        {
            InitializeComponent();
            StartApiServer();
            FormClosing += Form1_FormClosing;
            BuildLoginForm();
        }

        private void StartApiServer()
        {
            try
            {
                apiServer = new NotesApiServer(connectionString);
                apiServer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось запустить API заметок: " + ex.Message);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (apiServer != null)
                apiServer.Stop();
        }

        private void BuildLoginForm()
        {
            Controls.Clear();
            Text = "Авторизация";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(380, 270);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            Controls.Add(new Label { Text = "Вход в систему", Font = new Font("Segoe UI", 14, FontStyle.Bold), AutoSize = true, Location = new Point(115, 20) });
            Controls.Add(new Label { Text = "Логин:", Location = new Point(40, 70), AutoSize = true });
            txtLogin = new TextBox { Location = new Point(140, 67), Width = 170 };
            Controls.Add(txtLogin);
            Controls.Add(new Label { Text = "Пароль:", Location = new Point(40, 105), AutoSize = true });
            txtPassword = new TextBox { Location = new Point(140, 102), Width = 170, UseSystemPasswordChar = true };
            Controls.Add(txtPassword);
            btnLogin = new Button { Text = "Войти", Location = new Point(140, 140), Width = 100 };
            btnLogin.Click += BtnLogin_Click;
            Controls.Add(btnLogin);
            lblMessage = new Label { ForeColor = Color.Red, Location = new Point(40, 180), Size = new Size(300, 50) };
            Controls.Add(lblMessage);
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                lblMessage.Text = "Введите логин и пароль.";
                return;
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                int? userIdByLogin = GetUserIdByLogin(connection, login);

                string sql = @"
                    SELECT u.id, u.login, r.name AS role_name, u.is_blocked
                    FROM Users u
                    JOIN Roles r ON u.role_id = r.id
                    WHERE u.login = @login AND u.password_hash = @password";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@login", login);
                    command.Parameters.AddWithValue("@password", password);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            reader.Close();
                            failedAttempts++;

                            if (userIdByLogin.HasValue)
                                IncreaseFailedAttempts(connection, userIdByLogin.Value);

                            lblMessage.Text = failedAttempts >= 3
                                ? "Вы заблокированы. Обратитесь к администратору"
                                : "Вы ввели неверный логин или пароль. Пожалуйста проверьте ещё раз введенные данные";
                            return;
                        }

                        if ((bool)reader["is_blocked"])
                        {
                            lblMessage.Text = "Вы заблокированы. Обратитесь к администратору";
                            return;
                        }

                        currentUser = new CurrentUserData
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            Login = reader["login"].ToString(),
                            Role = reader["role_name"].ToString()
                        };
                    }
                }

                using (CaptchaForm captcha = new CaptchaForm())
                {
                    if (captcha.ShowDialog() != DialogResult.OK)
                    {
                        IncreaseFailedAttempts(connection, currentUser.Id);
                        lblMessage.Text = IsUserBlocked(connection, currentUser.Id)
                            ? "Вы заблокированы. Обратитесь к администратору"
                            : "Капча пройдена неверно.";
                        currentUser = null;
                        return;
                    }
                }

                ResetFailedAttempts(connection, currentUser.Id);
            }

            MessageBox.Show("Вы успешно авторизовались");
            BuildMainForm();
        }

        private int? GetUserIdByLogin(SqlConnection connection, string login)
        {
            using (SqlCommand command = new SqlCommand("SELECT id FROM Users WHERE login = @login", connection))
            {
                command.Parameters.AddWithValue("@login", login);
                object result = command.ExecuteScalar();
                return result == null ? (int?)null : Convert.ToInt32(result);
            }
        }

        private void IncreaseFailedAttempts(SqlConnection connection, int userId)
        {
            string sql = @"
                UPDATE Users
                SET failed_attempts = failed_attempts + 1,
                    is_blocked = CASE WHEN failed_attempts + 1 >= 3 THEN 1 ELSE is_blocked END
                WHERE id = @id";
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@id", userId);
                command.ExecuteNonQuery();
            }
        }

        private bool IsUserBlocked(SqlConnection connection, int userId)
        {
            using (SqlCommand command = new SqlCommand("SELECT is_blocked FROM Users WHERE id = @id", connection))
            {
                command.Parameters.AddWithValue("@id", userId);
                object result = command.ExecuteScalar();
                return result != null && Convert.ToBoolean(result);
            }
        }

        private void ResetFailedAttempts(SqlConnection connection, int userId)
        {
            using (SqlCommand command = new SqlCommand("UPDATE Users SET failed_attempts = 0 WHERE id = @id", connection))
            {
                command.Parameters.AddWithValue("@id", userId);
                command.ExecuteNonQuery();
            }
        }

        private void BuildMainForm()
        {
            Controls.Clear();
            Text = "Производственный учет";
            Size = new Size(1100, 650);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;

            Panel topPanel = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = SystemColors.Control };
            Label userLabel = new Label
            {
                Text = "Пользователь: " + currentUser.Login + " | Роль: " + currentUser.Role,
                Dock = DockStyle.Left,
                Width = 520,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Button btnExit = new Button { Text = "Выйти", Dock = DockStyle.Right, Width = 65 };
            btnExit.Click += (s, e) => { failedAttempts = 0; currentUser = null; BuildLoginForm(); };
            topPanel.Controls.Add(userLabel);
            topPanel.Controls.Add(btnExit);

            TabControl tabs = new TabControl { Dock = DockStyle.Fill, Appearance = TabAppearance.Normal, SizeMode = TabSizeMode.Normal };
            bool canEdit = currentUser.Role == "Администратор";

            tabs.TabPages.Add(HomeTabHelper.CreateHomeTab(currentUser.Login, currentUser.Role));
            tabs.TabPages.Add(OrderCrudHelper.CreateOrdersTab(connectionString, canEdit));
            tabs.TabPages.Add(OrderCrudHelper.CreateOrderItemsTab(connectionString, canEdit));
            tabs.TabPages.Add(CrudHelper.CreateDictionaryTab(connectionString, "Продукция", "Products", true, false, canEdit));
            tabs.TabPages.Add(CrudHelper.CreateDictionaryTab(connectionString, "Материалы", "Materials", true, true, canEdit));
            tabs.TabPages.Add(CrudHelper.CreateDictionaryTab(connectionString, "Операции", "Operations", false, true, canEdit));
            tabs.TabPages.Add(SpecificationCrudHelper.CreateSpecificationsTab(connectionString, canEdit));
            tabs.TabPages.Add(NoteCrudHelper.CreateNotesTab(connectionString, currentUser.Id, canEdit));
            tabs.TabPages.Add(CostTabHelper.CreateCostTab(connectionString));

            if (canEdit)
                tabs.TabPages.Add(UserTabHelper.CreateUsersTab(connectionString));

            Controls.Add(tabs);
            Controls.Add(topPanel);
        }
    }
}
