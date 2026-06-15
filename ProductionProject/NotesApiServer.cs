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

        public string Url { get; private set; } = "http://localhost:8080/api/notes/";

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
                if (path != "/api/notes")
                {
                    await WriteJson(context.Response, 400, "{\"error\":\"Некорректный адрес запроса\"}");
                    return;
                }

                string json = LoadNotesJson();
                await WriteJson(context.Response, 200, json);
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

            string sql = @"
                SELECT n.id, n.title, n.content, n.created_at, u.login
                FROM Notes n
                JOIN Users u ON n.user_id = u.id
                ORDER BY n.created_at DESC, n.id DESC";

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
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

        private async Task WriteJson(HttpListenerResponse response, int statusCode, string json)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            response.StatusCode = statusCode;
            response.ContentType = "application/json; charset=utf-8";
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private string EscapeJson(string value)
        {
            if (value == null) return "";
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }
    }
}
