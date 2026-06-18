using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace ProductionProject
{
    public partial class Form1 : Form
    {
        private readonly string connectionString = DbConnectionProvider.ConnectionString;

        private TextBox txtLogin;
        private TextBox txtPassword;
        private Button btnLogin;
        private Label lblMessage;

        private CurrentUserData currentUser;
        private NotesApiServer apiServer;

        private class CurrentUserData
        {
            public int Id;
            public string Login;
            public string Role;
            public int FailedAttempts;
        }

        public Form1()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterScreen;
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
                MessageBox.Show(
                    "Не удалось запустить JSON API заметок.\n\n" + ex.Message,
                    "Ошибка запуска API",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
            UiHelper.ApplyFormStyle(this);
            Text = "Авторизация";
            Size = new Size(1100, 650);
            MinimumSize = new Size(900, 560);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            CenterToScreen();

            Panel header = UiHelper.CreateTopPanel("Производственный учет", "Вход в информационную систему");
            Panel content = new Panel { Dock = DockStyle.Fill, BackColor = UiHelper.LightBackground, Padding = new Padding(0) };
            Panel card = new Panel { Size = new Size(420, 300), BackColor = Color.White, Padding = new Padding(28) };
            card.Anchor = AnchorStyles.None;
            card.Location = new Point((ClientSize.Width - card.Width) / 2, (ClientSize.Height - header.Height - card.Height) / 2);
            content.Resize += (s, e) => card.Location = new Point((content.ClientSize.Width - card.Width) / 2, (content.ClientSize.Height - card.Height) / 2);

            Label title = new Label
            {
                Text = "Вход в систему",
                Font = new Font("Segoe UI", 17, FontStyle.Bold),
                ForeColor = UiHelper.HeaderText,
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label subtitle = new Label
            {
                Text = "Введите логин и пароль для продолжения",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.DimGray,
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblLogin = new Label { Text = "Логин", Location = new Point(28, 90), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            txtLogin = new TextBox { Location = new Point(28, 112), Width = 364, Height = 26 };

            Label lblPassword = new Label { Text = "Пароль", Location = new Point(28, 148), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            txtPassword = new TextBox { Location = new Point(28, 170), Width = 364, Height = 26, UseSystemPasswordChar = true };

            btnLogin = UiHelper.CreateButton("Войти", 364);
            btnLogin.Location = new Point(28, 214);
            btnLogin.Height = 32;
            btnLogin.Click += BtnLogin_Click;

            lblMessage = new Label { ForeColor = Color.Red, Location = new Point(28, 252), Size = new Size(364, 40), TextAlign = ContentAlignment.TopCenter };

            card.Controls.Add(lblMessage);
            card.Controls.Add(btnLogin);
            card.Controls.Add(lblPassword);
            card.Controls.Add(txtPassword);
            card.Controls.Add(lblLogin);
            card.Controls.Add(txtLogin);
            card.Controls.Add(subtitle);
            card.Controls.Add(title);
            content.Controls.Add(card);
            Controls.Add(content);
            Controls.Add(header);
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

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    int? userIdByLogin = GetUserIdByLogin(connection, login);

                    const string sql = @"
SELECT u.user_id, u.user_login, r.role_name, u.is_blocked, u.failed_attempts
FROM Users u
JOIN Roles r ON u.role_id = r.role_id
WHERE u.user_login = @login
  AND u.password_hash = @password";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@login", login);
                        command.Parameters.AddWithValue("@password", password);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                reader.Close();
                                if (userIdByLogin.HasValue)
                                    IncreaseFailedAttempts(connection, userIdByLogin.Value, 1);

                                lblMessage.Text = userIdByLogin.HasValue && IsUserBlocked(connection, userIdByLogin.Value)
                                    ? "Вы заблокированы. Обратитесь к администратору"
                                    : "Вы ввели неверный логин или пароль. Проверьте введенные данные.";
                                return;
                            }

                            if ((bool)reader["is_blocked"])
                            {
                                lblMessage.Text = "Вы заблокированы. Обратитесь к администратору";
                                return;
                            }

                            currentUser = new CurrentUserData
                            {
                                Id = Convert.ToInt32(reader["user_id"]),
                                Login = reader["user_login"].ToString(),
                                Role = reader["role_name"].ToString(),
                                FailedAttempts = Convert.ToInt32(reader["failed_attempts"])
                            };
                        }
                    }

                    int remainingCaptchaAttempts = Math.Max(1, 3 - currentUser.FailedAttempts);
                    using (CaptchaForm captcha = new CaptchaForm(remainingCaptchaAttempts))
                    {
                        DialogResult result = captcha.ShowDialog();
                        if (result != DialogResult.OK)
                        {
                            int attemptsToAdd = captcha.FailedAttempts > 0 ? captcha.FailedAttempts : 1;
                            IncreaseFailedAttempts(connection, currentUser.Id, attemptsToAdd);
                            lblMessage.Text = IsUserBlocked(connection, currentUser.Id)
                                ? "Вы заблокированы. Обратитесь к администратору"
                                : "Капча не пройдена.";
                            currentUser = null;
                            return;
                        }
                    }

                    ResetFailedAttempts(connection, currentUser.Id);
                }
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Ошибка подключения к базе данных: " + ex.Message;
                return;
            }

            MessageBox.Show("Вы успешно авторизовались", "Авторизация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            BuildMainForm();
        }

        private int? GetUserIdByLogin(SqlConnection connection, string login)
        {
            using (SqlCommand command = new SqlCommand(
                "SELECT user_id FROM Users WHERE user_login = @login", connection))
            {
                command.Parameters.AddWithValue("@login", login);
                object result = command.ExecuteScalar();
                return result == null ? (int?)null : Convert.ToInt32(result);
            }
        }

        private void IncreaseFailedAttempts(SqlConnection connection, int userId, int count)
        {
            const string sql = @"
UPDATE Users
SET failed_attempts = failed_attempts + @count,
    is_blocked = CASE
        WHEN failed_attempts + @count >= 3 THEN 1
        ELSE is_blocked
    END
WHERE user_id = @userId";

            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@count", count);
                command.Parameters.AddWithValue("@userId", userId);
                command.ExecuteNonQuery();
            }
        }

        private bool IsUserBlocked(SqlConnection connection, int userId)
        {
            using (SqlCommand command = new SqlCommand(
                "SELECT is_blocked FROM Users WHERE user_id = @userId", connection))
            {
                command.Parameters.AddWithValue("@userId", userId);
                object result = command.ExecuteScalar();
                return result != null && Convert.ToBoolean(result);
            }
        }

        private void ResetFailedAttempts(SqlConnection connection, int userId)
        {
            using (SqlCommand command = new SqlCommand(
                "UPDATE Users SET failed_attempts = 0 WHERE user_id = @userId", connection))
            {
                command.Parameters.AddWithValue("@userId", userId);
                command.ExecuteNonQuery();
            }
        }

        private void BuildMainForm()
        {
            Controls.Clear();
            UiHelper.ApplyFormStyle(this);
            Text = "Производственный учет";
            Size = new Size(1100, 650);
            MinimumSize = new Size(900, 560);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            CenterToScreen();

            Panel topPanel = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = SystemColors.Control };
            Label userLabel = new Label
            {
                Text = "Пользователь: " + currentUser.Login + " | Роль: " + currentUser.Role,
                Dock = DockStyle.Left,
                Width = 520,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Button btnExit = new Button { Text = "Выйти", Dock = DockStyle.Right, Width = 65 };
            btnExit.Click += (s, e) => { currentUser = null; BuildLoginForm(); };
            topPanel.Controls.Add(userLabel);
            topPanel.Controls.Add(btnExit);

            TabControl tabs = new TabControl { Dock = DockStyle.Fill, Appearance = TabAppearance.Normal, SizeMode = TabSizeMode.Normal };
            bool isAdministrator = currentUser.Role == "Администратор";
            const bool canEditBusinessData = true;

            tabs.TabPages.Add(HomeTabHelper.CreateHomeTab(currentUser.Login, currentUser.Role));
            tabs.TabPages.Add(OrderCrudHelper.CreateOrdersTab(connectionString, canEditBusinessData));
            tabs.TabPages.Add(OrderCrudHelper.CreateOrderItemsTab(connectionString, canEditBusinessData));
            tabs.TabPages.Add(CrudHelper.CreateDictionaryTab(connectionString, "Продукция", "Products", true, false, canEditBusinessData));
            tabs.TabPages.Add(CrudHelper.CreateDictionaryTab(connectionString, "Материалы", "Materials", true, true, canEditBusinessData));
            tabs.TabPages.Add(CrudHelper.CreateDictionaryTab(connectionString, "Операции", "Operations", false, true, canEditBusinessData));
            tabs.TabPages.Add(SpecificationCrudHelper.CreateSpecificationsTab(connectionString, canEditBusinessData));
            tabs.TabPages.Add(NoteCrudHelper.CreateNotesTab(connectionString, currentUser.Id, canEditBusinessData));
            tabs.TabPages.Add(CostTabHelper.CreateCostTab(connectionString));

            if (isAdministrator)
                tabs.TabPages.Add(UserTabHelper.CreateUsersTab(connectionString));

            Controls.Add(tabs);
            Controls.Add(topPanel);
        }
    }
}
