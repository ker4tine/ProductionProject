using System;
using System.Configuration;

namespace ProductionProject
{
    public static class DbConnectionProvider
    {
        private const string ConnectionName = "PracticeDBConnectionString";

        public static string ConnectionString
        {
            get
            {
                ConnectionStringSettings settings =
                    ConfigurationManager.ConnectionStrings[ConnectionName];

                if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
                {
                    throw new InvalidOperationException(
                        "В App.config отсутствует строка подключения '" +
                        ConnectionName + "'.");
                }

                return settings.ConnectionString;
            }
        }
    }
}
