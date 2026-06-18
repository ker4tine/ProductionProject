USE PracticeDB;
GO

IF OBJECT_ID(N'dbo.ProductionOrders', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.ProductionOrders;
END;
GO
