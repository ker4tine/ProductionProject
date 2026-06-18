USE PracticeDB;
GO

INSERT INTO Roles (role_name)
VALUES
(N'Администратор'),
(N'Пользователь');
GO

INSERT INTO Users
(
    user_login,
    password_hash,
    full_name,
    role_id,
    is_blocked,
    failed_attempts
)
VALUES
(
    N'admin',
    N'admin123',
    N'Главный администратор',
    1,
    0,
    0
),
(
    N'user',
    N'user123',
    N'Обычный пользователь',
    2,
    0,
    0
);
GO

INSERT INTO Products
(
    product_code,
    product_name,
    unit_name
)
VALUES
(
    N'P001',
    N'Стол кухонный Самобранка',
    N'шт'
);
GO

INSERT INTO Materials
(
    material_code,
    material_name,
    unit_name,
    material_price
)
VALUES
(N'M001', N'Столешница круглая', N'шт', 3250),
(N'M002', N'Мебельная деталь 500x800', N'шт', 95),
(N'M003', N'Мебельная деталь 600x800', N'шт', 140),
(N'M004', N'Евровинт 6.5x50', N'шт', 595),
(N'M005', N'Опора', N'шт', 245);
GO

INSERT INTO Operations
(
    operation_code,
    operation_name,
    operation_price
)
VALUES
(N'O001', N'Сборка модулей', 1400),
(N'O002', N'Распил ДСП', 450),
(N'O003', N'Упаковка', 950);
GO

INSERT INTO Specifications
(
    product_id,
    material_id,
    operation_id,
    material_qty,
    operation_qty
)
VALUES
(1, 1, NULL, 1, 0),
(1, 2, NULL, 2, 0),
(1, 3, NULL, 1, 0),
(1, 4, NULL, 8, 0),
(1, 5, NULL, 4, 0),
(1, NULL, 1, 0, 1),
(1, NULL, 2, 0, 1),
(1, NULL, 3, 0, 1);
GO

INSERT INTO Notes
(
    user_id,
    note_title,
    note_content
)
VALUES
(
    1,
    N'Конференция ИТ',
    N'Подготовить материалы'
),
(
    2,
    N'Заказ мебели',
    N'Проверить расчёт стоимости'
),
(
    2,
    N'Практика',
    N'Завершить разработку системы'
);
GO