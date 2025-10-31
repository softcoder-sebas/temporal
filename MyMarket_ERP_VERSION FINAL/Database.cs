using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace MyMarket_ERP
{
    internal static class Database
    {
        private static bool _initialized;

        public static string ConnectionString { get; } =
            Environment.GetEnvironmentVariable("MYMARKET_SQLSERVER_CS")
            ?? "Server=localhost\\SQLEXPRESS;Database=MyMarketERP;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;\n";

        public static SqlConnection OpenConnection()
        {
            var cn = new SqlConnection(ConnectionString);
            cn.Open();
            return cn;
        }

        public static void EnsureInitialized()
        {
            if (_initialized)
                return;

            using var cn = OpenConnection();
            foreach (var sql in GetSchemaCommands())
            {
                using var cmd = new SqlCommand(sql, cn);
                cmd.ExecuteNonQuery();
            }

            foreach (var sql in GetSeedCommands())
            {
                using var cmd = new SqlCommand(sql, cn);
                cmd.ExecuteNonQuery();
            }

            _initialized = true;
        }

        private static IEnumerable<string> GetSchemaCommands()
        {
            yield return @"IF OBJECT_ID('dbo.Users','U') IS NULL
BEGIN
    CREATE TABLE dbo.Users(
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Email NVARCHAR(100) NOT NULL UNIQUE,
        Password NVARCHAR(64) NOT NULL,
        Role NVARCHAR(50) NOT NULL,
        IsActive BIT NOT NULL DEFAULT (1),
        CustomerId INT NULL
    );
END";

            yield return @"IF OBJECT_ID('dbo.Customers','U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers(
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(120) NOT NULL,
        Email NVARCHAR(100) NULL,
        Document NVARCHAR(40) NULL,
        Phone NVARCHAR(30) NULL,
        Address NVARCHAR(200) NULL,
        Visits INT NOT NULL DEFAULT(0),
        CreatedAt DATETIME2 NOT NULL DEFAULT(SYSDATETIME())
    );
    CREATE UNIQUE INDEX IX_Customers_Document ON dbo.Customers(Document) WHERE Document IS NOT NULL;
    CREATE INDEX IX_Customers_Name ON dbo.Customers(Name);
    CREATE INDEX IX_Customers_Visits ON dbo.Customers(Visits DESC);
END";

            yield return @"IF OBJECT_ID('dbo.Users','U') IS NOT NULL AND COL_LENGTH('dbo.Users','CustomerId') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD CustomerId INT NULL;
END";

            yield return @"IF OBJECT_ID('dbo.Users','U') IS NOT NULL AND OBJECT_ID('dbo.Customers','U') IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_Customers')
BEGIN
    ALTER TABLE dbo.Users WITH NOCHECK
    ADD CONSTRAINT FK_Users_Customers FOREIGN KEY (CustomerId)
    REFERENCES dbo.Customers(Id) ON DELETE SET NULL;
END";

            yield return @"IF OBJECT_ID('dbo.Users','U') IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_CustomerId' AND object_id = OBJECT_ID('dbo.Users'))
BEGIN
    CREATE INDEX IX_Users_CustomerId ON dbo.Users(CustomerId);
END";

            yield return @"IF OBJECT_ID('dbo.Invoices','U') IS NOT NULL AND COL_LENGTH('dbo.Invoices','CustomerEmail') IS NULL
BEGIN
    ALTER TABLE dbo.Invoices ADD CustomerEmail NVARCHAR(100) NULL;
END";

            yield return @"IF OBJECT_ID('dbo.Invoices','U') IS NOT NULL AND COL_LENGTH('dbo.Invoices','CustomerId') IS NULL
BEGIN
    ALTER TABLE dbo.Invoices ADD CustomerId INT NULL;
END";

            yield return @"IF OBJECT_ID('dbo.Invoices','U') IS NOT NULL AND COL_LENGTH('dbo.Invoices','PaymentStatus') IS NULL
BEGIN
    ALTER TABLE dbo.Invoices ADD PaymentStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_Invoices_PaymentStatus DEFAULT('Pagada');
END";

            yield return @"IF OBJECT_ID('dbo.Invoices','U') IS NOT NULL AND OBJECT_ID('dbo.Customers','U') IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Invoices_Customers')
BEGIN
    ALTER TABLE dbo.Invoices WITH NOCHECK
    ADD CONSTRAINT FK_Invoices_Customers FOREIGN KEY (CustomerId)
    REFERENCES dbo.Customers(Id) ON DELETE SET NULL;
END";

            yield return @"IF OBJECT_ID('dbo.Invoices','U') IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_Invoices_CustomerId' AND object_id = OBJECT_ID('dbo.Invoices'))
BEGIN
    CREATE INDEX IX_Invoices_CustomerId ON dbo.Invoices(CustomerId);
END";

        yield return @"IF OBJECT_ID('dbo.Employees','U') IS NULL
BEGIN
    CREATE TABLE dbo.Employees(
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(150) NOT NULL,
        Email NVARCHAR(120) NULL,
        Phone NVARCHAR(60) NULL,
        Department NVARCHAR(80) NULL,
        Position NVARCHAR(80) NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT('Activo'),
        Salary DECIMAL(18,2) NOT NULL DEFAULT(0),
        HireDate DATE NULL,
        DocumentNumber NVARCHAR(50) NULL,
        Address NVARCHAR(200) NULL,
        BankAccount NVARCHAR(80) NULL,
        EmergencyContact NVARCHAR(120) NULL,
        EmergencyPhone NVARCHAR(60) NULL,
        BirthDate DATE NULL,
        Gender NVARCHAR(20) NULL,
        MaritalStatus NVARCHAR(30) NULL,
        HealthProvider NVARCHAR(100) NULL,
        PensionProvider NVARCHAR(100) NULL,
        BloodType NVARCHAR(10) NULL,
        ContractType NVARCHAR(40) NULL,
        CompensationFund NVARCHAR(100) NULL,
        ContractDuration NVARCHAR(100) NULL
    );
END";

            yield return @"IF OBJECT_ID('dbo.Employees','U') IS NOT NULL AND COL_LENGTH('dbo.Employees','DocumentNumber') IS NULL
BEGIN
    ALTER TABLE dbo.Employees ADD DocumentNumber NVARCHAR(50) NULL;
END";

            yield return @"IF OBJECT_ID('dbo.Employees','U') IS NOT NULL AND COL_LENGTH('dbo.Employees','Address') IS NULL
BEGIN
    ALTER TABLE dbo.Employees ADD Address NVARCHAR(200) NULL;
END";

            yield return @"IF OBJECT_ID('dbo.Employees','U') IS NOT NULL AND COL_LENGTH('dbo.Employees','BankAccount') IS NULL
BEGIN
    ALTER TABLE dbo.Employees ADD BankAccount NVARCHAR(80) NULL;
END";

            yield return @"IF OBJECT_ID('dbo.Employees','U') IS NOT NULL AND COL_LENGTH('dbo.Employees','EmergencyContact') IS NULL
BEGIN
    ALTER TABLE dbo.Employees ADD EmergencyContact NVARCHAR(120) NULL;
END";

        yield return @"IF OBJECT_ID('dbo.Employees','U') IS NOT NULL AND COL_LENGTH('dbo.Employees','EmergencyPhone') IS NULL
BEGIN
    ALTER TABLE dbo.Employees ADD EmergencyPhone NVARCHAR(60) NULL;
END";

            yield return @"IF OBJECT_ID('dbo.Employees','U') IS NOT NULL AND COL_LENGTH('dbo.Employees','BirthDate') IS NULL
BEGIN
    ALTER TABLE dbo.Employees ADD BirthDate DATE NULL;
END";

            yield return @"IF OBJECT_ID('dbo.Employees','U') IS NOT NULL AND COL_LENGTH('dbo.Employees','Gender') IS NULL
BEGIN
    ALTER TABLE dbo.Employees ADD Gender NVARCHAR(20) NULL;
END";

            yield return @"IF OBJECT_ID('dbo.Employees','U') IS NOT NULL AND COL_LENGTH('dbo.Employees','MaritalStatus') IS NULL
BEGIN
    ALTER TABLE dbo.Employees ADD MaritalStatus NVARCHAR(30) NULL;
END";

            yield return @"IF OBJECT_ID('dbo.Employees','U') IS NOT NULL AND COL_LENGTH('dbo.Employees','HealthProvider') IS NULL
BEGIN
    ALTER TABLE dbo.Employees ADD HealthProvider NVARCHAR(100) NULL;
END";

            yield return @"IF OBJECT_ID('dbo.Employees','U') IS NOT NULL AND COL_LENGTH('dbo.Employees','PensionProvider') IS NULL
BEGIN
    ALTER TABLE dbo.Employees ADD PensionProvider NVARCHAR(100) NULL;
END";

            yield return @"IF OBJECT_ID('dbo.Employees','U') IS NOT NULL AND COL_LENGTH('dbo.Employees','BloodType') IS NULL
BEGIN
    ALTER TABLE dbo.Employees ADD BloodType NVARCHAR(10) NULL;
END";

            yield return @"IF OBJECT_ID('dbo.Employees','U') IS NOT NULL AND COL_LENGTH('dbo.Employees','ContractType') IS NULL
BEGIN
    ALTER TABLE dbo.Employees ADD ContractType NVARCHAR(40) NULL;
END";

            yield return @"IF OBJECT_ID('dbo.Employees','U') IS NOT NULL AND COL_LENGTH('dbo.Employees','CompensationFund') IS NULL
BEGIN
    ALTER TABLE dbo.Employees ADD CompensationFund NVARCHAR(100) NULL;
END";

            yield return @"IF OBJECT_ID('dbo.Employees','U') IS NOT NULL AND COL_LENGTH('dbo.Employees','ContractDuration') IS NULL
BEGIN
    ALTER TABLE dbo.Employees ADD ContractDuration NVARCHAR(100) NULL;
END";

            yield return @"IF OBJECT_ID('dbo.Employees','U') IS NOT NULL AND COL_LENGTH('dbo.Employees','Dependents') IS NOT NULL
BEGIN
    DECLARE @constraintName NVARCHAR(128);
    SELECT @constraintName = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.Employees') AND c.name = 'Dependents';
    IF @constraintName IS NOT NULL
    BEGIN
        DECLARE @escapedName NVARCHAR(258) = REPLACE(@constraintName, ']', ']]');
        DECLARE @sql NVARCHAR(MAX) = N'ALTER TABLE dbo.Employees DROP CONSTRAINT [' + @escapedName + N']';
        EXEC(@sql);
    END
    ALTER TABLE dbo.Employees DROP COLUMN Dependents;
END";

            yield return @"IF OBJECT_ID('dbo.EmployeePayments','U') IS NULL
BEGIN
    CREATE TABLE dbo.EmployeePayments(
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EmployeeId INT NOT NULL,
        Type NVARCHAR(20) NOT NULL,
        PeriodStart DATE NULL,
        PeriodEnd DATE NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Notes NVARCHAR(400) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT(SYSDATETIME()),
        CONSTRAINT FK_EmployeePayments_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_EmployeePayments_EmployeeId ON dbo.EmployeePayments(EmployeeId);
    CREATE INDEX IX_EmployeePayments_Type_PeriodEnd ON dbo.EmployeePayments(Type, PeriodEnd);
END";

            yield return @"IF OBJECT_ID('dbo.Products','U') IS NULL
BEGIN
    CREATE TABLE dbo.Products(
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Code NVARCHAR(40) NOT NULL UNIQUE,
        Name NVARCHAR(200) NOT NULL,
        Price DECIMAL(18,2) NOT NULL,
        Stock INT NOT NULL DEFAULT(0),
        IsActive BIT NOT NULL DEFAULT(1)
    );
END";

            yield return @"IF OBJECT_ID('dbo.Invoices','U') IS NULL
BEGIN
    CREATE TABLE dbo.Invoices(
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Number NVARCHAR(40) NOT NULL UNIQUE,
        IssuedAt DATETIME2 NOT NULL,
        CashierEmail NVARCHAR(120) NULL,
        Customer NVARCHAR(150) NULL,
        CustomerEmail NVARCHAR(100) NULL,
        CustomerId INT NULL,
        PaymentMethod NVARCHAR(40) NULL,
        PaymentStatus NVARCHAR(20) NOT NULL DEFAULT('Pagada'),
        Subtotal DECIMAL(18,2) NOT NULL,
        Tax DECIMAL(18,2) NOT NULL,
        Total DECIMAL(18,2) NOT NULL
    );
    CREATE INDEX IX_Invoices_IssuedAt ON dbo.Invoices(IssuedAt);
END";

            yield return @"IF OBJECT_ID('dbo.InvoiceItems','U') IS NULL
BEGIN
    CREATE TABLE dbo.InvoiceItems(
        Id INT IDENTITY(1,1) PRIMARY KEY,
        InvoiceId INT NOT NULL,
        ProductId INT NOT NULL,
        Code NVARCHAR(40) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Qty INT NOT NULL,
        Price DECIMAL(18,2) NOT NULL,
        Subtotal DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_InvoiceItems_Invoices FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoices(Id) ON DELETE CASCADE,
        CONSTRAINT FK_InvoiceItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id)
    );
    CREATE INDEX IX_InvoiceItems_InvoiceId ON dbo.InvoiceItems(InvoiceId);
END";
        }

        private static IEnumerable<string> GetSeedCommands()
        {
            yield return @"IF NOT EXISTS(SELECT 1 FROM dbo.Users)
BEGIN
    INSERT INTO dbo.Users(Email,Password,Role,CustomerId) VALUES
    ('admin@erp.local', LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256','Admin123'),2)), 'admin', NULL),
    ('conta@erp.local', LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256','1234'),2)), 'contable', NULL),
    ('caja@erp.local', LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256','1234'),2)), 'caja', NULL),
    ('inv@erp.local', LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256','1234'),2)), 'inventario', NULL),
    ('cli@erp.local', LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256','1234'),2)), 'cliente', NULL);
END";

            yield return @"UPDATE dbo.Users
SET Password = LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', Password),2))
WHERE LEN(Password) < 64;";

            yield return @"IF NOT EXISTS(SELECT 1 FROM dbo.Employees)
BEGIN
    INSERT INTO dbo.Employees
    (Name,Email,Phone,Department,Position,Status,Salary,HireDate,DocumentNumber,Address,BankAccount,EmergencyContact,EmergencyPhone,BirthDate,Gender,MaritalStatus,HealthProvider,PensionProvider,BloodType,ContractType,CompensationFund,ContractDuration) VALUES
    ('Ana García','ana.garcia@empresa.com','+57 3234567891','Ventas','Gerente de Ventas','Activo',2500000,'2023-03-15','CC 52.123.456','Cra 10 #20-30, Bogotá','Banco de Bogotá - 0123456789','Luis García','+57 3214567890','1990-05-18','Femenino','Casada','Sanitas EPS','Porvenir','O+','Indefinido','Compensar','Indefinido'),
    ('Carlos López','carlos.lopez@empresa.com','+57 3122223344','Almacén','Supervisor Almacén','Vacaciones',1500000,'2022-08-10','CC 80.456.789','Calle 45 #12-40, Medellín','Bancolombia - 9876543210','Marcela Pérez','+57 3009876543','1988-09-02','Masculino','Casado','Sura EPS','Protección','A+','Indefinido','Comfama','Indefinido'),
    ('María Rodríguez','maria.rod@empresa.com','+57 3001112233','Caja','Cajera','Activo',1200000,'2024-02-01','CC 65.987.321','Av. 3 #45-67, Cali','Davivienda - 4561237890','Andrés Rodríguez','+57 3106547891','1995-01-22','Femenino','Soltera','Nueva EPS','Colpensiones','B+','Término fijo','Comfenalco Valle','12 meses'),
    ('Julián Fernández','jfernandez@empresa.com','+57 3158765432','Tecnología','Analista de Soporte','Activo',1800000,'2021-11-05','CC 79.654.123','Carrera 52 #14-09, Bogotá','Banco Caja Social - 7412589630','Paula Ríos','+57 3503214567','1987-07-11','Masculino','Casado','Compensar EPS','Skandia','O-','Indefinido','Cafam','Indefinido'),
    ('Laura Méndez','laura.mendez@empresa.com','+57 3012233445','Recursos Humanos','Coordinadora RRHH','Activo',3100000,'2020-04-13','CC 43.210.987','Calle 98 #15-30, Bogotá','BBVA - 3698521470','Camilo Méndez','+57 3019988776','1985-12-05','Femenino','Casada','Aliansalud EPS','Colfondos','AB+','Indefinido','Compensar','Indefinido'),
    ('Sofía Torres','sofia.torres@empresa.com','+57 3204455667','Logística','Auxiliar Logística','Activo',1400000,'2024-01-08','CC 1.023.456.789','Diagonal 75 #8-19, Barranquilla','Banco Popular - 2589631470','Elena Torres','+57 3201122334','1998-03-29','Femenino','Soltera','Salud Total EPS','Protección','A-','Aprendizaje','Comfamiliar Atlántico','6 meses');
END";

            yield return @"IF NOT EXISTS(SELECT 1 FROM dbo.Products)
BEGIN
    INSERT INTO dbo.Products(Code,Name,Price,Stock) VALUES
    ('1001','Arroz 1Kg',5500,100),
    ('1002','Azúcar 1Kg',4800,80),
    ('1003','Leche 1L',4200,120),
    ('2001','Aceite 900ml',9800,60),
    ('3001','Panela 500g',3500,90);
END";

            yield return @"UPDATE dbo.Invoices
SET CustomerEmail = Customer
WHERE CustomerEmail IS NULL AND Customer LIKE '%@%';";

            yield return @"UPDATE i
SET CustomerId = c.Id
FROM dbo.Invoices i
JOIN dbo.Customers c ON i.CustomerId IS NULL
    AND c.Email IS NOT NULL AND c.Email <> ''
    AND (ISNULL(i.CustomerEmail,'') = c.Email OR ISNULL(i.Customer,'') = c.Email OR ISNULL(i.Customer,'') = c.Name);";
        }
    }
}
