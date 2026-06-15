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
            Panel card = new Panel { Dock = DockStyle.Top, Height = 180, BackColor = Color.White, Padding = new Padding(22) };

            string roleText = role == "Администратор"
                ? "Доступны просмотр данных, управление пользователями и редактирование справочников/заказов."
                : "Доступен просмотр производственных данных.";

            Label label = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                Text = "Пользователь: " + login + "\nРоль: " + role + "\n\n" + roleText + "\n\nAPI заметок: http://localhost:8080/api/notes\nВеб-представление заметок: http://localhost:8080/notes"
            };

            card.Controls.Add(label);
            content.Controls.Add(card);
            page.Controls.Add(content);
            page.Controls.Add(UiHelper.CreateTopPanel("Производственный учет", "Информационная система учета заказов, спецификаций и заметок"));
            return page;
        }
    }
}
