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

        private class CurrentUserData
        {
            public int Id;
            public string Login;
            public string Role;
        }

        public Form1()
        {
            InitializeComponent();
            BuildLoginForm();
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

            if (failedAttempts >= 2)
            {
                using (CaptchaForm captcha = new CaptchaForm())
                {
                    if (captcha.ShowDialog() != DialogResult.OK)
                    {
                        lblMessage.Text = "Капча пройдена неверно.";
                        return;
                    }
                }
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

                            if (failedAttempts == 1)
                                lblMessage.Text = "Неверный логин или пароль. Осталось 2 попытки.";
                            else if (failedAttempts == 2)
                                lblMessage.Text = "Неверный логин или пароль. При следующей попытке появится капча.";
                            else
                                lblMessage.Text = "Пользователь заблокирован после 3 ошибок. Обратитесь к администратору.";

                            return;
                        }

                        if ((bool)reader["is_blocked"])
                        {
                            lblMessage.Text = "Пользователь заблокирован.";
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

                ResetFailedAttempts(connection, currentUser.Id);
            }

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
            Button btnExit = new Button { Text = "Выйти", Dock = DockStyle.Right, Width = 90 };
            btnExit.Click += (s, e) => { failedAttempts = 0; currentUser = null; BuildLoginForm(); };
            topPanel.Controls.Add(userLabel);
            topPanel.Controls.Add(btnExit);

            TabControl tabs = new TabControl { Dock = DockStyle.Fill, Appearance = TabAppearance.Normal, SizeMode = TabSizeMode.Normal };
            bool canEdit = currentUser.Role == "Администратор";

            tabs.TabPages.Add(CreateHomeTab());
            tabs.TabPages.Add(OrderCrudHelper.CreateOrdersTab(connectionString, canEdit));
            tabs.TabPages.Add(OrderCrudHelper.CreateOrderItemsTab(connectionString, canEdit));
            tabs.TabPages.Add(CrudHelper.CreateDictionaryTab(connectionString, "Продукция", "Products", true, false, canEdit));
            tabs.TabPages.Add(CrudHelper.CreateDictionaryTab(connectionString, "Материалы", "Materials", true, true, canEdit));
            tabs.TabPages.Add(CrudHelper.CreateDictionaryTab(connectionString, "Операции", "Operations", false, true, canEdit));

            tabs.TabPages.Add(CreateTableTab("Спецификации", @"
                SELECT p.name AS [Продукция],
                       ISNULL(m.name, o.name) AS [Материал или операция],
                       CASE WHEN m.id IS NOT NULL THEN N'Материал' ELSE N'Операция' END AS [Тип],
                       CASE WHEN m.id IS NOT NULL THEN s.material_qty ELSE s.operation_qty END AS [Количество],
                       ISNULL(m.unit, N'операция') AS [Ед. изм.],
                       ISNULL(m.price, o.price) AS [Цена]
                FROM Specifications s
                JOIN Products p ON s.product_id = p.id
                LEFT JOIN Materials m ON s.material_id = m.id
                LEFT JOIN Operations o ON s.operation_id = o.id"));

            tabs.TabPages.Add(CreateTableTab("Заметки", @"
                SELECT n.title AS [Заголовок], u.login AS [Пользователь], n.content AS [Содержание], FORMAT(n.created_at, 'dd.MM.yyyy') AS [Дата]
                FROM Notes n JOIN Users u ON n.user_id = u.id"));

            tabs.TabPages.Add(CreateCostTab());

            if (canEdit)
                tabs.TabPages.Add(CreateUsersTab());

            Controls.Add(tabs);
            Controls.Add(topPanel);
        }

        private TabPage CreateHomeTab()
        {
            TabPage page = new TabPage("Главная");
            Label label = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                Text = currentUser.Role == "Администратор"
                    ? "Вы вошли как администратор. Доступны просмотр данных, управление пользователями и редактирование справочников/заказов."
                    : "Вы вошли как пользователь. Доступен только просмотр производственных данных."
            };
            page.Controls.Add(label);
            return page;
        }

        private TabPage CreateUsersTab()
        {
            TabPage page = new TabPage("Пользователи");
            DataGridView grid = CreateGrid();
            FlowLayoutPanel panel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38 };

            Button btnRefresh = new Button { Text = "Обновить", Width = 100 };
            Button btnAdd = new Button { Text = "Добавить", Width = 100 };
            Button btnBlock = new Button { Text = "Заблокировать", Width = 120 };
            Button btnUnblock = new Button { Text = "Разблокировать", Width = 130 };
            Button btnReset = new Button { Text = "Сбросить ошибки", Width = 130 };

            string query = "SELECT u.id AS [ID], u.login AS [Логин], u.full_name AS [ФИО], r.name AS [Роль], CASE WHEN u.is_blocked = 1 THEN N'Да' ELSE N'Нет' END AS [Заблокирован], u.failed_attempts AS [Ошибок входа] FROM Users u JOIN Roles r ON u.role_id = r.id";
            btnRefresh.Click += (s, e) => LoadGrid(grid, query);
            btnAdd.Click += (s, e) => AddUser(grid, query);
            btnBlock.Click += (s, e) => SetSelectedUserBlocked(grid, true, query);
            btnUnblock.Click += (s, e) => SetSelectedUserBlocked(grid, false, query);
            btnReset.Click += (s, e) => ResetSelectedUserAttempts(grid, query);

            panel.Controls.Add(btnRefresh);
            panel.Controls.Add(btnAdd);
            panel.Controls.Add(btnBlock);
            panel.Controls.Add(btnUnblock);
            panel.Controls.Add(btnReset);
            page.Controls.Add(grid);
            page.Controls.Add(panel);
            LoadGrid(grid, query);
            return page;
        }

        private void AddUser(DataGridView grid, string query)
        {
            using (UserEditForm form = new UserEditForm())
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand("INSERT INTO Users (login, password_hash, full_name, role_id) VALUES (@login, @password, @name, @role)", connection))
                {
                    command.Parameters.AddWithValue("@login", form.UserLogin);
                    command.Parameters.AddWithValue("@password", form.UserPassword);
                    command.Parameters.AddWithValue("@name", form.FullName);
                    command.Parameters.AddWithValue("@role", form.RoleId);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                LoadGrid(grid, query);
            }
        }

        private int GetSelectedUserId(DataGridView grid)
        {
            if (grid.CurrentRow == null) return 0;
            return Convert.ToInt32(grid.CurrentRow.Cells["ID"].Value);
        }

        private void SetSelectedUserBlocked(DataGridView grid, bool blocked, string query)
        {
            int id = GetSelectedUserId(grid);
            if (id == 0) return;
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand("UPDATE Users SET is_blocked = @blocked WHERE id = @id", connection))
            {
                command.Parameters.AddWithValue("@blocked", blocked);
                command.Parameters.AddWithValue("@id", id);
                connection.Open();
                command.ExecuteNonQuery();
            }
            LoadGrid(grid, query);
        }

        private void ResetSelectedUserAttempts(DataGridView grid, string query)
        {
            int id = GetSelectedUserId(grid);
            if (id == 0) return;
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand("UPDATE Users SET failed_attempts = 0, is_blocked = 0 WHERE id = @id", connection))
            {
                command.Parameters.AddWithValue("@id", id);
                connection.Open();
                command.ExecuteNonQuery();
            }
            LoadGrid(grid, query);
        }

        private TabPage CreateTableTab(string title, string query)
        {
            TabPage page = new TabPage(title);
            DataGridView grid = CreateGrid();
            Button refreshButton = new Button { Text = "Обновить", Dock = DockStyle.Top, Height = 32 };
            refreshButton.Click += (s, e) => LoadGrid(grid, query);
            page.Controls.Add(grid);
            page.Controls.Add(refreshButton);
            LoadGrid(grid, query);
            return page;
        }

        private DataGridView CreateGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
            };
        }

        private TabPage CreateCostTab()
        {
            string query = @"
                SELECT c.name AS [Заказчик], p.name AS [Продукция], coi.quantity AS [Количество],
                       SUM(coi.quantity * (ISNULL(s.material_qty, 0) * ISNULL(m.price, 0) + ISNULL(s.operation_qty, 0) * ISNULL(o.price, 0))) AS [Полная стоимость]
                FROM CustomerOrders co
                JOIN Counterparties c ON co.customer_id = c.id
                JOIN CustomerOrderItems coi ON co.id = coi.order_id
                JOIN Products p ON coi.product_id = p.id
                JOIN Specifications s ON coi.product_id = s.product_id
                LEFT JOIN Materials m ON s.material_id = m.id
                LEFT JOIN Operations o ON s.operation_id = o.id
                GROUP BY c.name, p.name, coi.quantity";
            return CreateTableTab("Расчет стоимости", query);
        }

        private void LoadGrid(DataGridView grid, string query)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    grid.DataSource = table;
                    if (grid.Columns.Contains("ID"))
                        grid.Columns["ID"].Visible = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
        }
    }
}
