using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Windows.Forms;

namespace ProductionProject
{
    public static class NoteCrudHelper
    {
        private const string NotesPageUrl = "http://localhost:8080/notes";
        private const string NotesJsonUrl = "http://localhost:8080/api/notes";

        public static TabPage CreateNotesTab(string connectionString, int currentUserId, bool canEdit)
        {
            string query = @"
                SELECT n.id AS [ID], n.title AS [Заголовок], u.login AS [Пользователь], n.content AS [Содержание], FORMAT(n.created_at, 'dd.MM.yyyy') AS [Дата]
                FROM Notes n
                JOIN Users u ON n.user_id = u.id
                ORDER BY n.created_at DESC, n.id DESC";

            TabPage page = new TabPage("Заметки") { BackColor = UiHelper.LightBackground };
            BindingSource source = new BindingSource();
            DataGridView grid = UiHelper.CreateGrid();
            FlowLayoutPanel toolbar = UiHelper.CreateToolbar();

            if (canEdit)
            {
                Button btnAdd = UiHelper.CreateButton("Добавить", 100);
                Button btnEdit = UiHelper.CreateButton("Изменить", 100);
                Button btnDelete = UiHelper.CreateButton("Удалить", 100);

                btnAdd.Click += (s, e) => AddNote(connectionString, currentUserId, grid, source, query);
                btnEdit.Click += (s, e) => EditNote(connectionString, grid, source, query);
                btnDelete.Click += (s, e) => DeleteNote(connectionString, grid, source, query);

                toolbar.Controls.Add(btnAdd);
                toolbar.Controls.Add(btnEdit);
                toolbar.Controls.Add(btnDelete);
            }

            Button btnRefresh = UiHelper.CreateButton("Обновить", 100);
            Button btnOpenPage = UiHelper.CreateButton("Веб-представление", 160);
            Button btnOpenJson = UiHelper.CreateButton("API JSON", 120);

            btnRefresh.Click += (s, e) => LoadGrid(connectionString, grid, source, query);
            btnOpenPage.Click += (s, e) => OpenUrl(NotesPageUrl);
            btnOpenJson.Click += (s, e) => OpenUrl(NotesJsonUrl);

            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(btnOpenPage);
            toolbar.Controls.Add(btnOpenJson);
            UiHelper.AddStartsWithSearch(toolbar, source, "Заголовок", "Поиск по заголовку:");

            Panel tablePanel = UiHelper.CreateTablePanel(grid, source);
            Panel content = new Panel { Dock = DockStyle.Fill, BackColor = UiHelper.LightBackground, Padding = new Padding(16) };
            content.Controls.Add(tablePanel);
            content.Controls.Add(toolbar);

            page.Controls.Add(content);
            page.Controls.Add(UiHelper.CreateTopPanel("Заметки производственной системы", "Работа с заметками и встроенным API"));

            LoadGrid(connectionString, grid, source, query);
            return page;
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось открыть ссылку: " + ex.Message);
            }
        }

        private static void LoadGrid(string connectionString, DataGridView grid, BindingSource source, string query)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    UiHelper.BindTable(grid, source, table);
                    HideIdColumn(grid);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки заметок: " + ex.Message);
            }
        }

        private static void HideIdColumn(DataGridView grid)
        {
            if (grid.Columns.Contains("ID")) grid.Columns["ID"].Visible = false;
        }

        private static int GetSelectedId(DataGridView grid)
        {
            if (grid.CurrentRow == null)
            {
                MessageBox.Show("Выберите строку.");
                return 0;
            }
            return Convert.ToInt32(grid.CurrentRow.Cells["ID"].Value);
        }

        private static void AddNote(string connectionString, int currentUserId, DataGridView grid, BindingSource source, string query)
        {
            using (NoteEditForm form = new NoteEditForm("Добавление заметки"))
            {
                if (form.ShowDialog() != DialogResult.OK) return;

                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand("INSERT INTO Notes (user_id, title, content, created_at) VALUES (@user, @title, @content, GETDATE())", connection))
                {
                    command.Parameters.AddWithValue("@user", currentUserId);
                    command.Parameters.AddWithValue("@title", form.NoteTitle);
                    command.Parameters.AddWithValue("@content", form.NoteContent);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                LoadGrid(connectionString, grid, source, query);
            }
        }

        private static void EditNote(string connectionString, DataGridView grid, BindingSource source, string query)
        {
            int id = GetSelectedId(grid);
            if (id == 0) return;

            string title = Convert.ToString(grid.CurrentRow.Cells["Заголовок"].Value);
            string content = Convert.ToString(grid.CurrentRow.Cells["Содержание"].Value);

            using (NoteEditForm form = new NoteEditForm("Изменение заметки", title, content))
            {
                if (form.ShowDialog() != DialogResult.OK) return;

                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand("UPDATE Notes SET title=@title, content=@content WHERE id=@id", connection))
                {
                    command.Parameters.AddWithValue("@title", form.NoteTitle);
                    command.Parameters.AddWithValue("@content", form.NoteContent);
                    command.Parameters.AddWithValue("@id", id);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                LoadGrid(connectionString, grid, source, query);
            }
        }

        private static void DeleteNote(string connectionString, DataGridView grid, BindingSource source, string query)
        {
            int id = GetSelectedId(grid);
            if (id == 0) return;
            if (MessageBox.Show("Удалить выбранную заметку?", "Подтверждение", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand("DELETE FROM Notes WHERE id=@id", connection))
            {
                command.Parameters.AddWithValue("@id", id);
                connection.Open();
                command.ExecuteNonQuery();
            }
            LoadGrid(connectionString, grid, source, query);
        }
    }
}
