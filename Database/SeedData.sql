USE PracticeDB;
GO

INSERT INTO Roles (role_name)
VALUES (N'Администратор'), (N'Пользователь');
GO

DECLARE @AdminRoleId INT;
DECLARE @UserRoleId INT;
SELECT @AdminRoleId = role_id FROM Roles WHERE role_name = N'Администратор';
SELECT @UserRoleId = role_id FROM Roles WHERE role_name = N'Пользователь';

INSERT INTO Users (user_login, password_hash, full_name, role_id, is_blocked, failed_attempts)
VALUES
(N'admin', N'admin123', N'Главный администратор', @AdminRoleId, 0, 0),
(N'user', N'user123', N'Обычный пользователь', @UserRoleId, 0, 0);
GO

INSERT INTO Products (product_code, product_name, unit_name)
VALUES (N'P001', N'Стол кухонный Самобранка', N'шт');
GO

INSERT INTO Materials (material_code, material_name, unit_name, material_price)
VALUES
(N'M001', N'Столешница круглая', N'шт', 3250),
(N'M002', N'Мебельная деталь 500x800', N'шт', 95),
(N'M003', N'Мебельная деталь 600x800', N'шт', 140),
(N'M004', N'Евровинт 6.5x50', N'шт', 595),
(N'M005', N'Опора', N'шт', 245);
GO

INSERT INTO Operations (operation_code, operation_name, operation_price)
VALUES
(N'O001', N'Сборка модулей', 1400),
(N'O002', N'Распил ДСП', 450),
(N'O003', N'Упаковка', 950);
GO

DECLARE @ProductId INT;
DECLARE @Material1 INT;
DECLARE @Material2 INT;
DECLARE @Material3 INT;
DECLARE @Material4 INT;
DECLARE @Material5 INT;
DECLARE @Operation1 INT;
DECLARE @Operation2 INT;
DECLARE @Operation3 INT;

SELECT @ProductId = product_id FROM Products WHERE product_code = N'P001';
SELECT @Material1 = material_id FROM Materials WHERE material_code = N'M001';
SELECT @Material2 = material_id FROM Materials WHERE material_code = N'M002';
SELECT @Material3 = material_id FROM Materials WHERE material_code = N'M003';
SELECT @Material4 = material_id FROM Materials WHERE material_code = N'M004';
SELECT @Material5 = material_id FROM Materials WHERE material_code = N'M005';
SELECT @Operation1 = operation_id FROM Operations WHERE operation_code = N'O001';
SELECT @Operation2 = operation_id FROM Operations WHERE operation_code = N'O002';
SELECT @Operation3 = operation_id FROM Operations WHERE operation_code = N'O003';

INSERT INTO Specifications (product_id, material_id, operation_id, material_qty, operation_qty)
VALUES
(@ProductId, @Material1, NULL, 1, 0),
(@ProductId, @Material2, NULL, 2, 0),
(@ProductId, @Material3, NULL, 1, 0),
(@ProductId, @Material4, NULL, 8, 0),
(@ProductId, @Material5, NULL, 4, 0),
(@ProductId, NULL, @Operation1, 0, 1),
(@ProductId, NULL, @Operation2, 0, 1),
(@ProductId, NULL, @Operation3, 0, 1);
GO

DECLARE @AdminUserId INT;
DECLARE @UserId INT;
SELECT @AdminUserId = user_id FROM Users WHERE user_login = N'admin';
SELECT @UserId = user_id FROM Users WHERE user_login = N'user';

INSERT INTO Notes (user_id, note_title, note_content)
VALUES
(@AdminUserId, N'Конференция ИТ', N'Подготовить материалы'),
(@UserId, N'Заказ мебели', N'Проверить расчёт стоимости'),
(@UserId, N'Практика', N'Завершить разработку системы');
GO
