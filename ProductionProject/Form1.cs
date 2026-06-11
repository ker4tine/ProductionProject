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
        private int currentUserId;
        private string currentLogin;
        private string currentRole;

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
            Size = new Size(360, 260);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            Label lblTitle = new Label { Text = "Вход в систему", Font = new Font("Segoe UI", 14, FontStyle.Bold), AutoSize = true, Location = new Point(105, 20) };
            Label lblLogin = new Label { Text = "Логин:", Location = new Point(40, 70), AutoSize = true };
            txtLogin = new TextBox { Location = new Point(130, 67), Width = 160 };
            Label lblPassword = new Label { Text = "Пароль:", Location = new Point(40, 105), AutoSize = true };
            txtPassword = new TextBox { Location = new Point(130, 102), Width = 160, UseSystemPasswordChar = true };
            btnLogin = new Button { Text = "Войти", Location = new Point(130, 140), Width = 100 };
            btnLogin.Click += BtnLogin_Click;
            lblMessage = new Label { Text = "", ForeColor = Color.Red, Location = new Point(40, 180), Size = new Size(270, 40) };

            Controls.Add(lblTitle);
            Controls.Add(lblLogin);
            Controls.Add(txtLogin);
            Controls.Add(lblPassword);
            Controls.Add(txtPassword);
            Controls.Add(btnLogin);
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
                            {
                                IncreaseFailedAttempts(connection, userIdByLogin.Value);
                            }

                            lblMessage.Text = failedAttempts >= 3
                                ? "Пользователь заблокирован после 3 ошибок. Обратитесь к администратору."
                                : "Неверный логин или пароль.";
                            return;
                        }

                        if ((bool)reader["is_blocked"])
                        {
                            lblMessage.Text = "Пользователь заблокирован.";
                            return;
                        }

                        currentUserId = Convert.ToInt32(reader["id"]);
                        currentLogin = reader["login"].ToString();
                        currentRole = reader["role_name"].ToString();
                    }
                }

                ResetFailedAttempts(connection, currentUserId);
            }

            BuildMainForm();
        }

        private int? GetUserIdByLogin(SqlConnection connection, string login)
        {
            using (SqlCommand command = new SqlCommand("SELECT id FROM Users WHERE login = @login", connection))
            {
                command.Parameters.AddWithValue("@login", login);
                object result = command.ExecuteScalar();
                if (result == null) return null;
                return Convert.ToInt32(result);
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
            Size = new Size(1000, 650);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;

            Label lblUser = new Label { Text = "Пользователь: " + currentLogin + " | Роль: " + currentRole, Dock = DockStyle.Top, Height = 30, TextAlign = ContentAlignment.MiddleLeft };
            TabControl tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(CreateTableTab("Заказы", "SELECT id AS [Номер заказа], customer_id AS [Код заказчика], order_date AS [Дата заказа] FROM CustomerOrders"));
            tabs.TabPages.Add(CreateTableTab("Позиции заказов", "SELECT id AS [Номер позиции], order_id AS [Номер заказа], product_id AS [Код продукции], quantity AS [Количество] FROM CustomerOrderItems"));
            tabs.TabPages.Add(CreateTableTab("Продукция", "SELECT id AS [Код], code AS [Артикул], name AS [Наименование], unit AS [Единица измерения] FROM Products"));
            tabs.TabPages.Add(CreateTableTab("Материалы", "SELECT id AS [Код], code AS [Артикул], name AS [Наименование], unit AS [Единица измерения], price AS [Цена] FROM Materials"));
            tabs.TabPages.Add(CreateTableTab("Операции", "SELECT id AS [Код], code AS [Артикул], name AS [Наименование], price AS [Цена] FROM Operations"));
            tabs.TabPages.Add(CreateTableTab("Спецификации", "SELECT id AS [Код], product_id AS [Код продукции], material_id AS [Код материала], operation_id AS [Код операции], material_qty AS [Количество материала], operation_qty AS [Количество операции] FROM Specifications"));
            tabs.TabPages.Add(CreateTableTab("Заметки", "SELECT n.id AS [Код], n.title + N' - ' + u.login AS [Заголовок и пользователь], n.content AS [Содержание], FORMAT(n.created_at, 'dd.MM.yyyy') AS [Дата] FROM Notes n JOIN Users u ON n.user_id = u.id"));
            tabs.TabPages.Add(CreateCostTab());

            if (currentRole == "Администратор")
            {
                tabs.TabPages.Add(CreateUsersTab());
            }

            Controls.Add(tabs);
            Controls.Add(lblUser);
        }

        private TabPage CreateUsersTab()
        {
            TabPage page = new TabPage("Пользователи");
            DataGridView grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false };
            FlowLayoutPanel panel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38 };

            Button btnRefresh = new Button { Text = "Обновить", Width = 100 };
            Button btnAdd = new Button { Text = "Добавить", Width = 100 };
            Button btnBlock = new Button { Text = "Заблокировать", Width = 120 };
            Button btnUnblock = new Button { Text = "Разблокировать", Width = 130 };
            Button btnReset = new Button { Text = "Сбросить ошибки", Width = 130 };

            string query = "SELECT u.id AS [Код], u.login AS [Логин], u.full_name AS [ФИО], r.name AS [Роль], CASE WHEN u.is_blocked = 1 THEN N'Да' ELSE N'Нет' END AS [Заблокирован], u.failed_attempts AS [Ошибок входа] FROM Users u JOIN Roles r ON u.role_id = r.id";
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
            return Convert.ToInt32(grid.CurrentRow.Cells["Код"].Value);
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
            DataGridView grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AllowUserToAddRows = false };
            Button refreshButton = new Button { Text = "Обновить", Dock = DockStyle.Top, Height = 32 };
            refreshButton.Click += (s, e) => LoadGrid(grid, query);
            page.Controls.Add(grid);
            page.Controls.Add(refreshButton);
            LoadGrid(grid, query);
            return page;
        }

        private TabPage CreateCostTab()
        {
            string query = @"
                SELECT co.id AS [Номер заказа],
                       SUM(coi.quantity * (ISNULL(s.material_qty, 0) * ISNULL(m.price, 0) + ISNULL(s.operation_qty, 0) * ISNULL(o.price, 0))) AS [Полная стоимость]
                FROM CustomerOrders co
                JOIN CustomerOrderItems coi ON co.id = coi.order_id
                JOIN Specifications s ON coi.product_id = s.product_id
                LEFT JOIN Materials m ON s.material_id = m.id
                LEFT JOIN Operations o ON s.operation_id = o.id
                GROUP BY co.id";
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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
        }
    }
}
