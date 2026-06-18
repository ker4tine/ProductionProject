using System.Drawing;
using System.Windows.Forms;

namespace ProductionProject
{
    public static class HomeTabHelper
    {
        public static TabPage CreateHomeTab(string login, string role)
        {
            TabPage page = new TabPage("Главная") { BackColor = UiHelper.LightBackground };
            Panel content = new Panel { Dock = DockStyle.Fill, BackColor = UiHelper.LightBackground, Padding = new Padding(22) };
            Panel card = new Panel { Dock = DockStyle.Top, Height = 210, BackColor = Color.White, Padding = new Padding(22) };

            string roleText = role == "Администратор"
                ? "Доступны работа с производственными данными и администрирование пользователей."
                : "Доступна работа с заказами, справочниками, спецификациями и заметками.";

            Label label = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                Text = "Пользователь: " + login + "\nРоль: " + role + "\n\n" + roleText +
                       "\n\nJSON API заметок: http://localhost:8080/api/notes"
            };

            card.Controls.Add(label);
            content.Controls.Add(card);
            page.Controls.Add(content);
            page.Controls.Add(UiHelper.CreateTopPanel("Производственный учет", "Информационная система учета заказов, спецификаций и заметок"));
            return page;
        }
    }
}
