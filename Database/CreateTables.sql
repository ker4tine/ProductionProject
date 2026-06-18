USE PracticeDB;
GO

CREATE TABLE Roles
(
    role_id INT IDENTITY(1,1) PRIMARY KEY,
    role_name NVARCHAR(50) NOT NULL UNIQUE
);
GO

CREATE TABLE Users
(
    user_id INT IDENTITY(1,1) PRIMARY KEY,
    user_login NVARCHAR(100) NOT NULL UNIQUE,
    password_hash NVARCHAR(255) NOT NULL,
    full_name NVARCHAR(150) NULL,
    role_id INT NOT NULL,
    is_blocked BIT NOT NULL DEFAULT 0,
    failed_attempts INT NOT NULL DEFAULT 0,
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    FOREIGN KEY (role_id)
        REFERENCES Roles(role_id)
);
GO

CREATE TABLE Counterparties
(
    counterparty_id NVARCHAR(50) PRIMARY KEY,
    counterparty_name NVARCHAR(255) NOT NULL,
    inn NVARCHAR(20) NULL,
    address NVARCHAR(255) NULL,
    phone NVARCHAR(50) NULL,
    counterparty_type NVARCHAR(50) NOT NULL
);
GO

CREATE TABLE Products
(
    product_id INT IDENTITY(1,1) PRIMARY KEY,
    product_code NVARCHAR(50) NULL,
    product_name NVARCHAR(255) NOT NULL,
    unit_name NVARCHAR(50) NOT NULL
);
GO

CREATE TABLE Materials
(
    material_id INT IDENTITY(1,1) PRIMARY KEY,
    material_code NVARCHAR(50) NULL,
    material_name NVARCHAR(255) NOT NULL,
    unit_name NVARCHAR(50) NOT NULL,
    material_price MONEY NOT NULL DEFAULT 0,

    CHECK (material_price >= 0)
);
GO

CREATE TABLE Operations
(
    operation_id INT IDENTITY(1,1) PRIMARY KEY,
    operation_code NVARCHAR(50) NULL,
    operation_name NVARCHAR(255) NOT NULL,
    operation_price MONEY NOT NULL DEFAULT 0,

    CHECK (operation_price >= 0)
);
GO

CREATE TABLE Specifications
(
    specification_id INT IDENTITY(1,1) PRIMARY KEY,
    product_id INT NOT NULL,
    material_id INT NULL,
    operation_id INT NULL,
    material_qty FLOAT NOT NULL DEFAULT 0,
    operation_qty FLOAT NOT NULL DEFAULT 0,

    FOREIGN KEY (product_id)
        REFERENCES Products(product_id),

    FOREIGN KEY (material_id)
        REFERENCES Materials(material_id),

    FOREIGN KEY (operation_id)
        REFERENCES Operations(operation_id),

    CHECK (material_qty >= 0),
    CHECK (operation_qty >= 0),

    CHECK
    (
        material_id IS NOT NULL
        OR operation_id IS NOT NULL
    )
);
GO

CREATE TABLE CustomerOrders
(
    customer_order_id INT IDENTITY(1,1) PRIMARY KEY,
    customer_id NVARCHAR(50) NOT NULL,
    order_date DATE NOT NULL,

    FOREIGN KEY (customer_id)
        REFERENCES Counterparties(counterparty_id)
);
GO

CREATE TABLE CustomerOrderItems
(
    customer_order_item_id INT IDENTITY(1,1) PRIMARY KEY,
    customer_order_id INT NOT NULL,
    product_id INT NOT NULL,
    quantity FLOAT NOT NULL,

    FOREIGN KEY (customer_order_id)
        REFERENCES CustomerOrders(customer_order_id),

    FOREIGN KEY (product_id)
        REFERENCES Products(product_id),

    CHECK (quantity > 0)
);
GO

CREATE TABLE ProductionOrders
(
    production_order_id INT IDENTITY(1,1) PRIMARY KEY,
    customer_order_id INT NULL,
    product_id INT NOT NULL,
    quantity FLOAT NOT NULL,
    production_date DATE NOT NULL,

    FOREIGN KEY (customer_order_id)
        REFERENCES CustomerOrders(customer_order_id),

    FOREIGN KEY (product_id)
        REFERENCES Products(product_id),

    CHECK (quantity > 0)
);
GO

CREATE TABLE Notes
(
    note_id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    note_title NVARCHAR(255) NOT NULL,
    note_content NVARCHAR(MAX) NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    FOREIGN KEY (user_id)
        REFERENCES Users(user_id)
);
GO