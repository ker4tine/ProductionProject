using System;
using System.Data.SqlClient;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProductionProject
{
    public class NotesApiServer
    {
        private readonly string connectionString;
        private readonly HttpListener listener;
        private CancellationTokenSource cancellationTokenSource;
        private Task serverTask;

        public string Url { get; private set; } = "http://localhost:8080/";
        public string JsonUrl { get; private set; } = "http://localhost:8080/api/notes/";
        public string PageUrl { get; private set; } = "http://localhost:8080/notes/";

        public NotesApiServer(string connectionString)
        {
            this.connectionString = connectionString;
            listener = new HttpListener();
            listener.Prefixes.Add(Url);
        }

        public void Start()
        {
            if (listener.IsListening) return;

            cancellationTokenSource = new CancellationTokenSource();
            listener.Start();
            serverTask = Task.Run(() => ListenLoop(cancellationTokenSource.Token));
        }

        public void Stop()
        {
            try
            {
                if (cancellationTokenSource != null)
                    cancellationTokenSource.Cancel();

                if (listener.IsListening)
                    listener.Stop();
            }
            catch
            {
            }
        }

        private async Task ListenLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested && listener.IsListening)
            {
                try
                {
                    HttpListenerContext context = await listener.GetContextAsync();
                    await ProcessRequest(context);
                }
                catch
                {
                    if (!listener.IsListening) break;
                }
            }
        }

        private async Task ProcessRequest(HttpListenerContext context)
        {
            try
            {
                if (context.Request.HttpMethod != "GET")
                {
                    await WriteJson(context.Response, 400, "{\"error\":\"Некорректный метод запроса\"}");
                    return;
                }

                string path = context.Request.Url.AbsolutePath.TrimEnd('/').ToLower();
                if (path == "") path = "/";

                if (path == "/api/notes")
                {
                    await WriteJson(context.Response, 200, LoadNotesJson());
                    return;
                }

                if (path == "/" || path == "/notes")
                {
                    await WriteHtml(context.Response, 200, LoadNotesHtml());
                    return;
                }

                await WriteJson(context.Response, 400, "{\"error\":\"Некорректный адрес запроса\"}");
            }
            catch (Exception ex)
            {
                string message = EscapeJson(ex.Message);
                await WriteJson(context.Response, 500, "{\"error\":\"Ошибка сервера: " + message + "\"}");
            }
        }

        private string LoadNotesJson()
        {
            StringBuilder json = new StringBuilder();
            json.Append("[");
            bool first = true;

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(GetNotesSql(), connection))
            {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!first) json.Append(",");
                        first = false;

                        int id = Convert.ToInt32(reader["id"]);
                        string title = Convert.ToString(reader["title"]);
                        string login = Convert.ToString(reader["login"]);
                        string content = Convert.ToString(reader["content"]);
                        DateTime createdAt = Convert.ToDateTime(reader["created_at"]);

                        json.Append("{");
                        json.Append("\"id\":").Append(id).Append(",");
                        json.Append("\"title_user\":\"").Append(EscapeJson(title + " - " + login)).Append("\",");
                        json.Append("\"content\":\"").Append(EscapeJson(content)).Append("\",");
                        json.Append("\"formatted_date\":\"").Append(createdAt.ToString("dd.MM.yyyy")).Append("\"");
                        json.Append("}");
                    }
                }
            }

            json.Append("]");
            return json.ToString();
        }

        private string LoadNotesHtml()
        {
            StringBuilder html = new StringBuilder();
            html.Append("<!doctype html><html lang='ru'><head><meta charset='utf-8'>");
            html.Append("<meta name='viewport' content='width=device-width, initial-scale=1'>");
            html.Append("<title>Заметки производственной системы</title>");
            html.Append("<style>");
            html.Append("body{margin:0;font-family:Segoe UI,Arial,sans-serif;background:#f3f6fb;color:#1f2937}");
            html.Append("header{background:#2563eb;color:white;padding:28px 40px}");
            html.Append("main{padding:30px 40px}.card{background:white;border-radius:14px;padding:22px;box-shadow:0 8px 25px rgba(15,23,42,.08)}");
            html.Append("h1{margin:0 0 8px;font-size:28px}.muted{color:#dbeafe;margin:0}.api{margin:0 0 18px;color:#64748b}");
            html.Append("a{color:#2563eb;text-decoration:none;font-weight:600}table{width:100%;border-collapse:collapse;overflow:hidden;border-radius:10px}");
            html.Append("th{background:#eff6ff;text-align:left;color:#1e40af}th,td{padding:13px 14px;border-bottom:1px solid #e5e7eb;vertical-align:top}");
            html.Append("tr:hover{background:#f8fafc}.empty{text-align:center;padding:30px;color:#64748b}.id{width:70px}.date{width:130px;white-space:nowrap}");
            html.Append("</style></head><body>");
            html.Append("<header><h1>Заметки производственной системы</h1><p class='muted'>Удобный просмотр данных встроенного API</p></header>");
            html.Append("<main><div class='card'>");
            html.Append("<p class='api'>JSON-версия API: <a href='/api/notes'>/api/notes</a></p>");
            html.Append("<table><thead><tr><th class='id'>ID</th><th>Заголовок и пользователь</th><th>Содержание</th><th class='date'>Дата</th></tr></thead><tbody>");

            bool hasRows = false;
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(GetNotesSql(), connection))
            {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        hasRows = true;
                        int id = Convert.ToInt32(reader["id"]);
                        string title = Convert.ToString(reader["title"]);
                        string login = Convert.ToString(reader["login"]);
                        string content = Convert.ToString(reader["content"]);
                        DateTime createdAt = Convert.ToDateTime(reader["created_at"]);

                        html.Append("<tr>");
                        html.Append("<td class='id'>").Append(id).Append("</td>");
                        html.Append("<td>").Append(EscapeHtml(title + " - " + login)).Append("</td>");
                        html.Append("<td>").Append(EscapeHtml(content)).Append("</td>");
                        html.Append("<td class='date'>").Append(createdAt.ToString("dd.MM.yyyy")).Append("</td>");
                        html.Append("</tr>");
                    }
                }
            }

            if (!hasRows)
                html.Append("<tr><td colspan='4' class='empty'>Заметок пока нет</td></tr>");

            html.Append("</tbody></table></div></main></body></html>");
            return html.ToString();
        }

        private string GetNotesSql()
        {
            return @"
                SELECT n.id, n.title, n.content, n.created_at, u.login
                FROM Notes n
                JOIN Users u ON n.user_id = u.id
                ORDER BY n.created_at DESC, n.id DESC";
        }

        private async Task WriteJson(HttpListenerResponse response, int statusCode, string json)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            response.StatusCode = statusCode;
            response.ContentType = "application/json; charset=utf-8";
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private async Task WriteHtml(HttpListenerResponse response, int statusCode, string html)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(html);
            response.StatusCode = statusCode;
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private string EscapeJson(string value)
        {
            if (value == null) return "";
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }

        private string EscapeHtml(string value)
        {
            if (value == null) return "";
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;");
        }
    }
}
