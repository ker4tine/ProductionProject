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
        private const string NotesQuery = @"
SELECT n.note_id AS [ID], n.note_title AS [Заголовок],
       u.user_login AS [Пользователь], n.note_content AS [Содержание],
       FORMAT(n.created_at, 'dd.MM.yyyy') AS [Дата]
FROM Notes n
JOIN Users u ON n.user_id = u.user_id
ORDER BY n.created_at DESC, n.note_id DESC";

        public static TabPage CreateNotesTab(string connectionString, int currentUserId, bool canEdit)
        {
            TabPage page = new TabPage("Заметки") { BackColor = UiHelper.LightBackground };
            BindingSource source = new BindingSource();
            DataGridView grid = UiHelper.CreateGrid();
            FlowLayoutPanel toolbar = UiHelper.CreateToolbar();

            if (canEdit)
            {
                Button add = UiHelper.CreateButton("Добавить", 100);
                Button edit = UiHelper.CreateButton("Изменить", 100);
                Button delete = UiHelper.CreateButton("Удалить", 100);
                add.Click += (s, e) => AddNote(connectionString, currentUserId, grid, source);
                edit.Click += (s, e) => EditNote(connectionString, grid, source);
                delete.Click += (s, e) => DeleteNote(connectionString, grid, source);
                toolbar.Controls.Add(add); toolbar.Controls.Add(edit); toolbar.Controls.Add(delete);
            }

            Button refresh = UiHelper.CreateButton("Обновить", 100);
            Button openPage = UiHelper.CreateButton("Веб-представление", 160);
            Button openJson = UiHelper.CreateButton("API JSON", 120);
            refresh.Click += (s, e) => LoadGrid(connectionString, grid, source);
            openPage.Click += (s, e) => OpenUrl(NotesPageUrl);
            openJson.Click += (s, e) => OpenUrl(NotesJsonUrl);
            toolbar.Controls.Add(refresh); toolbar.Controls.Add(openPage); toolbar.Controls.Add(openJson);
            UiHelper.AddStartsWithSearch(toolbar, source, "Заголовок", "Поиск по заголовку:");

            Panel content = new Panel { Dock = DockStyle.Fill, BackColor = UiHelper.LightBackground, Padding = new Padding(16) };
            content.Controls.Add(UiHelper.CreateTablePanel(grid, source));
            content.Controls.Add(toolbar);
            page.Controls.Add(content);
            page.Controls.Add(UiHelper.CreateTopPanel("Заметки производственной системы", "Работа с заметками и встроенным API"));
            LoadGrid(connectionString, grid, source);
            return page;
        }

        private static void OpenUrl(string url)
        {
            try { Process.Start(url); }
            catch (Exception ex) { MessageBox.Show("Не удалось открыть ссылку: " + ex.Message); }
        }

        private static void LoadGrid(string cs, DataGridView grid, BindingSource source)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(cs))
                using (SqlDataAdapter adapter = new SqlDataAdapter(NotesQuery, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    UiHelper.BindTable(grid, source, table);
                    if (grid.Columns.Contains("ID")) grid.Columns["ID"].Visible = false;
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка загрузки заметок: " + ex.Message); }
        }

        private static int SelectedId(DataGridView grid)
        {
            if (grid.CurrentRow == null) { MessageBox.Show("Выберите строку."); return 0; }
            return Convert.ToInt32(grid.CurrentRow.Cells["ID"].Value);
        }

        private static void AddNote(string cs, int userId, DataGridView grid, BindingSource source)
        {
            using (NoteEditForm form = new NoteEditForm("Добавление заметки"))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                using (SqlConnection connection = new SqlConnection(cs))
                using (SqlCommand command = new SqlCommand("INSERT INTO Notes (user_id, note_title, note_content, created_at) VALUES (@user,@title,@content,GETDATE())", connection))
                {
                    command.Parameters.AddWithValue("@user", userId);
                    command.Parameters.AddWithValue("@title", form.NoteTitle);
                    command.Parameters.AddWithValue("@content", form.NoteContent);
                    connection.Open(); command.ExecuteNonQuery();
                }
                LoadGrid(cs, grid, source);
            }
        }

        private static void EditNote(string cs, DataGridView grid, BindingSource source)
        {
            int id = SelectedId(grid); if (id == 0) return;
            string title = Convert.ToString(grid.CurrentRow.Cells["Заголовок"].Value);
            string content = Convert.ToString(grid.CurrentRow.Cells["Содержание"].Value);
            using (NoteEditForm form = new NoteEditForm("Изменение заметки", title, content))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                using (SqlConnection connection = new SqlConnection(cs))
                using (SqlCommand command = new SqlCommand("UPDATE Notes SET note_title=@title, note_content=@content WHERE note_id=@id", connection))
                {
                    command.Parameters.AddWithValue("@title", form.NoteTitle);
                    command.Parameters.AddWithValue("@content", form.NoteContent);
                    command.Parameters.AddWithValue("@id", id);
                    connection.Open(); command.ExecuteNonQuery();
                }
                LoadGrid(cs, grid, source);
            }
        }

        private static void DeleteNote(string cs, DataGridView grid, BindingSource source)
        {
            int id = SelectedId(grid); if (id == 0) return;
            if (MessageBox.Show("Удалить выбранную заметку?", "Подтверждение", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            using (SqlConnection connection = new SqlConnection(cs))
            using (SqlCommand command = new SqlCommand("DELETE FROM Notes WHERE note_id=@id", connection))
            {
                command.Parameters.AddWithValue("@id", id);
                connection.Open(); command.ExecuteNonQuery();
            }
            LoadGrid(cs, grid, source);
        }
    }
}
