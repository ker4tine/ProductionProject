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
            Panel card = new Panel { Dock = DockStyle.Top, Height = 280, BackColor = Color.White, Padding = new Padding(22) };

            string roleText = role == "Администратор"
                ? "Доступны просмотр данных, управление пользователями и редактирование справочников/заказов."
                : "Доступен просмотр производственных данных.";

            Label label = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                Text = "Пользователь: " + login + "\nРоль: " + role + "\n\n" + roleText +
                       "\n\nAPI заметок: http://localhost:8080/api/notes" +
                       "\nВеб-представление заметок: http://localhost:8080/notes"
            };

            FlowLayoutPanel buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 42,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            Button counterpartiesButton = UiHelper.CreateButton("Контрагенты и импорт JSON", 220);
            counterpartiesButton.Height = 34;
            counterpartiesButton.Click += (s, e) => ShowTabInWindow(
                "Контрагенты",
                CounterpartyImportHelper.CreateCounterpartiesTab(DbConnectionProvider.ConnectionString, role == "Администратор"));

            Button productionOrdersButton = UiHelper.CreateButton("Производственные заказы", 220);
            productionOrdersButton.Height = 34;
            productionOrdersButton.Click += (s, e) => ShowTabInWindow(
                "Производственные заказы",
                ProductionOrderCrudHelper.CreateProductionOrdersTab(DbConnectionProvider.ConnectionString, role == "Администратор"));

            buttons.Controls.Add(counterpartiesButton);
            buttons.Controls.Add(productionOrdersButton);
            card.Controls.Add(label);
            card.Controls.Add(buttons);
            content.Controls.Add(card);
            page.Controls.Add(content);
            page.Controls.Add(UiHelper.CreateTopPanel("Производственный учет", "Информационная система учета заказов, спецификаций и заметок"));
            return page;
        }

        private static void ShowTabInWindow(string title, TabPage tab)
        {
            using (Form form = new Form())
            {
                form.Text = title;
                form.StartPosition = FormStartPosition.CenterParent;
                form.Size = new Size(1050, 620);
                form.MinimumSize = new Size(850, 520);

                while (tab.Controls.Count > 0)
                    form.Controls.Add(tab.Controls[0]);

                form.ShowDialog();
            }
        }
    }
}
