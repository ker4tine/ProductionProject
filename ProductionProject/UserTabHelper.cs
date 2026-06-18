using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace ProductionProject
{
    public static class UserTabHelper
    {
        private const string Query = @"
SELECT u.user_id AS [ID], u.user_login AS [Логин], u.full_name AS [ФИО],
       u.role_id AS [RoleID], r.role_name AS [Роль],
       CASE WHEN u.is_blocked = 1 THEN N'Да' ELSE N'Нет' END AS [Заблокирован],
       u.failed_attempts AS [Ошибок входа]
FROM Users u
JOIN Roles r ON u.role_id = r.role_id
ORDER BY u.user_login";

        public static TabPage CreateUsersTab(string connectionString)
        {
            TabPage page = new TabPage("Пользователи") { BackColor = UiHelper.LightBackground };
            BindingSource source = new BindingSource();
            DataGridView grid = UiHelper.CreateGrid();
            grid.DataBindingComplete += (s, e) => HideServiceColumns(grid);
            FlowLayoutPanel toolbar = UiHelper.CreateToolbar();

            Button add = UiHelper.CreateButton("Добавить", 100);
            Button edit = UiHelper.CreateButton("Изменить", 100);
            Button delete = UiHelper.CreateButton("Удалить", 100);
            Button block = UiHelper.CreateButton("Заблокировать", 130);
            Button unblock = UiHelper.CreateButton("Разблокировать", 140);
            Button reset = UiHelper.CreateButton("Сбросить ошибки", 150);

            add.Click += (s, e) => AddUser(connectionString, grid, source);
            edit.Click += (s, e) => EditUser(connectionString, grid, source);
            delete.Click += (s, e) => DeleteUser(connectionString, grid, source);
            block.Click += (s, e) => SetBlocked(connectionString, grid, source, true);
            unblock.Click += (s, e) => SetBlocked(connectionString, grid, source, false);
            reset.Click += (s, e) => ResetAttempts(connectionString, grid, source);

            toolbar.Controls.Add(add);
            toolbar.Controls.Add(edit);
            toolbar.Controls.Add(delete);
            toolbar.Controls.Add(block);
            toolbar.Controls.Add(unblock);
            toolbar.Controls.Add(reset);
            UiHelper.AddStartsWithSearch(toolbar, source, "Логин", "Поиск по логину:");

            Panel content = new Panel { Dock = DockStyle.Fill, BackColor = UiHelper.LightBackground, Padding = new Padding(16) };
            content.Controls.Add(UiHelper.CreateTablePanel(grid, source));
            content.Controls.Add(toolbar);
            page.Controls.Add(content);
            page.Controls.Add(UiHelper.CreateTopPanel("Пользователи", "Управление учетными записями и блокировками"));
            LoadGrid(connectionString, grid, source);
            return page;
        }

        private static void LoadGrid(string cs, DataGridView grid, BindingSource source)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(cs))
                using (SqlDataAdapter adapter = new SqlDataAdapter(Query, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    UiHelper.BindTable(grid, source, table);
                    HideServiceColumns(grid);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки пользователей: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void AddUser(string cs, DataGridView grid, BindingSource source)
        {
            using (UserEditForm form = new UserEditForm(cs))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                if (LoginExists(cs, form.UserLogin, 0))
                {
                    MessageBox.Show("Пользователь с таким логином уже существует.", "Проверка данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Execute(cs, "INSERT INTO Users (user_login, password_hash, full_name, role_id) VALUES (@login,@password,@name,@role)", command =>
                {
                    command.Parameters.AddWithValue("@login", form.UserLogin);
                    command.Parameters.AddWithValue("@password", form.UserPassword);
                    command.Parameters.AddWithValue("@name", form.FullName);
                    command.Parameters.AddWithValue("@role", form.RoleId);
                });
                LoadGrid(cs, grid, source);
            }
        }

        private static void EditUser(string cs, DataGridView grid, BindingSource source)
        {
            int id = SelectedUserId(grid); if (id == 0) return;
            string login = Convert.ToString(grid.CurrentRow.Cells["Логин"].Value);
            string fullName = Convert.ToString(grid.CurrentRow.Cells["ФИО"].Value);
            int roleId = Convert.ToInt32(grid.CurrentRow.Cells["RoleID"].Value);

            using (UserEditForm form = new UserEditForm(cs, login, fullName, roleId))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                if (LoginExists(cs, form.UserLogin, id))
                {
                    MessageBox.Show("Пользователь с таким логином уже существует.", "Проверка данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string sql = string.IsNullOrWhiteSpace(form.UserPassword)
                    ? "UPDATE Users SET user_login=@login, full_name=@name, role_id=@role WHERE user_id=@id"
                    : "UPDATE Users SET user_login=@login, password_hash=@password, full_name=@name, role_id=@role WHERE user_id=@id";

                Execute(cs, sql, command =>
                {
                    command.Parameters.AddWithValue("@login", form.UserLogin);
                    command.Parameters.AddWithValue("@name", form.FullName);
                    command.Parameters.AddWithValue("@role", form.RoleId);
                    command.Parameters.AddWithValue("@id", id);
                    if (!string.IsNullOrWhiteSpace(form.UserPassword))
                        command.Parameters.AddWithValue("@password", form.UserPassword);
                });
                LoadGrid(cs, grid, source);
            }
        }

        private static void DeleteUser(string cs, DataGridView grid, BindingSource source)
        {
            int id = SelectedUserId(grid); if (id == 0) return;
            if (MessageBox.Show("Удалить выбранного пользователя?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                Execute(cs, "DELETE FROM Users WHERE user_id=@id", command => command.Parameters.AddWithValue("@id", id));
                LoadGrid(cs, grid, source);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось удалить пользователя. Возможно, с ним связаны заметки.\n\n" + ex.Message,
                    "Ошибка удаления", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static bool LoginExists(string cs, string login, int excludedUserId)
        {
            using (SqlConnection connection = new SqlConnection(cs))
            using (SqlCommand command = new SqlCommand("SELECT COUNT(*) FROM Users WHERE user_login=@login AND user_id<>@id", connection))
            {
                command.Parameters.AddWithValue("@login", login);
                command.Parameters.AddWithValue("@id", excludedUserId);
                connection.Open();
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private static int SelectedUserId(DataGridView grid)
        {
            if (grid.CurrentRow == null)
            {
                MessageBox.Show("Выберите пользователя.", "Данные не выбраны", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return 0;
            }
            return Convert.ToInt32(grid.CurrentRow.Cells["ID"].Value);
        }

        private static void SetBlocked(string cs, DataGridView grid, BindingSource source, bool blocked)
        {
            int id = SelectedUserId(grid); if (id == 0) return;
            Execute(cs, "UPDATE Users SET is_blocked=@blocked WHERE user_id=@id", command =>
            {
                command.Parameters.AddWithValue("@blocked", blocked);
                command.Parameters.AddWithValue("@id", id);
            });
            LoadGrid(cs, grid, source);
        }

        private static void ResetAttempts(string cs, DataGridView grid, BindingSource source)
        {
            int id = SelectedUserId(grid); if (id == 0) return;
            Execute(cs, "UPDATE Users SET failed_attempts=0, is_blocked=0 WHERE user_id=@id",
                command => command.Parameters.AddWithValue("@id", id));
            LoadGrid(cs, grid, source);
        }

        private static void Execute(string cs, string sql, Action<SqlCommand> configure)
        {
            using (SqlConnection connection = new SqlConnection(cs))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                configure(command);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private static void HideServiceColumns(DataGridView grid)
        {
            foreach (string name in new[] { "ID", "RoleID" })
                if (grid.Columns.Contains(name)) grid.Columns[name].Visible = false;
        }
    }
}
