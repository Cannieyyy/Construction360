using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Construction360.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' not found.");

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                CreateTables();
                SeedInitialData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database initialization error: {ex.Message}");
                throw;
            }
        }

        // ============================================================
        // CREATE DATABASE TABLES
        // ============================================================

        private void CreateTables()
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            // EMPLOYEES TABLE (must exist before Users FK is created)
            ExecuteNonQuery(connection, @"
                IF OBJECT_ID('dbo.Employees', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.Employees (
                        Employee_ID INT PRIMARY KEY IDENTITY(1,1),
                        First_Name NVARCHAR(50) NOT NULL,
                        Last_Name NVARCHAR(50) NOT NULL,
                        ID_Number NVARCHAR(20) UNIQUE NOT NULL,
                        Phone_Number NVARCHAR(20),
                        Position NVARCHAR(50),
                        Department NVARCHAR(50),
                        Date_Hired DATE,
                        QRCode_Value NVARCHAR(255) UNIQUE,
                        IsActive BIT DEFAULT 1
                    );
                END");

            Console.WriteLine("Employees table checked.");

            // USERS TABLE
            ExecuteNonQuery(connection, @"
                IF OBJECT_ID('dbo.Users', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.Users (
                        User_ID INT PRIMARY KEY IDENTITY(1,1),
                        FullName NVARCHAR(100) NOT NULL,
                        Username NVARCHAR(50) UNIQUE NOT NULL,
                        Email NVARCHAR(100) UNIQUE NOT NULL,
                        PasswordHash NVARCHAR(255) NOT NULL,
                        Salt NVARCHAR(100) NOT NULL,
                        Role NVARCHAR(20) NOT NULL,
                        EmployeeId NVARCHAR(20) UNIQUE,
                        Employee_ID INT NULL,
                        Department NVARCHAR(50),
                        Position NVARCHAR(50),
                        IsActive BIT DEFAULT 0,
                        CreatedDate DATETIME DEFAULT GETDATE(),
                        LastLoginDate DATETIME,
                        AccountStatus NVARCHAR(20) DEFAULT 'Pending',
                        CONSTRAINT FK_Users_Employees
                            FOREIGN KEY (Employee_ID)
                            REFERENCES dbo.Employees(Employee_ID)
                    );
                END");

            ExecuteNonQuery(connection, @"
                IF COL_LENGTH('dbo.Users', 'AccountStatus') IS NULL
                BEGIN
                    ALTER TABLE dbo.Users ADD AccountStatus NVARCHAR(20) NULL;
                END");

            ExecuteNonQuery(connection, @"
                IF COL_LENGTH('dbo.Users', 'Employee_ID') IS NULL
                BEGIN
                    ALTER TABLE dbo.Users ADD Employee_ID INT NULL;
                END");

            ExecuteNonQuery(connection, @"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'FK_Users_Employees'
                      AND parent_object_id = OBJECT_ID('dbo.Users')
                )
                BEGIN
                    ALTER TABLE dbo.Users
                    ADD CONSTRAINT FK_Users_Employees
                        FOREIGN KEY (Employee_ID)
                        REFERENCES dbo.Employees(Employee_ID);
                END");

            ExecuteNonQuery(connection, @"
                UPDATE dbo.Users
                SET AccountStatus =
                    CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Pending' END
                WHERE AccountStatus IS NULL;");

            ExecuteNonQuery(connection, @"
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.default_constraints dc
                    INNER JOIN sys.columns c
                        ON c.object_id = dc.parent_object_id
                        AND c.column_id = dc.parent_column_id
                    WHERE dc.parent_object_id = OBJECT_ID('dbo.Users')
                      AND c.name = 'AccountStatus'
                )
                BEGIN
                    ALTER TABLE dbo.Users
                    ADD CONSTRAINT DF_Users_AccountStatus
                    DEFAULT 'Pending' FOR AccountStatus;
                END");

            ExecuteNonQuery(connection, @"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Users_Role'
                )
                BEGIN
                    ALTER TABLE dbo.Users WITH NOCHECK
                    ADD CONSTRAINT CK_Users_Role
                        CHECK (Role IN ('Admin','Supervisor','Employee'));
                END");

            ExecuteNonQuery(connection, @"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Users_AccountStatus'
                )
                BEGIN
                    ALTER TABLE dbo.Users WITH NOCHECK
                    ADD CONSTRAINT CK_Users_AccountStatus
                        CHECK (AccountStatus IN ('Active','Pending','Suspended','Disabled'));
                END");

            Console.WriteLine("Users table constraints checked.");

            // ATTENDANCE TABLE
            ExecuteNonQuery(connection, @"
                IF OBJECT_ID('dbo.Attendance', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.Attendance (
                        Attendance_ID INT PRIMARY KEY IDENTITY(1,1),
                        Employee_ID INT NOT NULL,
                        Date DATE NOT NULL,
                        CheckInTime DATETIME,
                        CheckOutTime DATETIME,
                        Status NVARCHAR(20),
                        FOREIGN KEY (Employee_ID)
                            REFERENCES dbo.Employees(Employee_ID)
                    );
                END");

            // LEAVE REQUESTS TABLE
            ExecuteNonQuery(connection, @"
                IF OBJECT_ID('dbo.LeaveRequests', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.LeaveRequests (
                        Leave_ID INT PRIMARY KEY IDENTITY(1,1),
                        Employee_ID INT NOT NULL,
                        Type NVARCHAR(20) NOT NULL,
                        Status NVARCHAR(20) NOT NULL,
                        StartDate DATE NOT NULL,
                        EndDate DATE NOT NULL,
                        Reason NVARCHAR(500),
                        SubmittedDate DATETIME DEFAULT GETDATE(),
                        FOREIGN KEY (Employee_ID)
                            REFERENCES dbo.Employees(Employee_ID)
                    );
                END");

            // NOTIFICATIONS TABLE
            ExecuteNonQuery(connection, @"
                IF OBJECT_ID('dbo.Notifications', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.Notifications (
                        Notification_ID INT PRIMARY KEY IDENTITY(1,1),
                        User_ID INT,
                        Title NVARCHAR(100) NOT NULL,
                        Message NVARCHAR(MAX) NOT NULL,
                        Type NVARCHAR(20),
                        DateSent DATETIME DEFAULT GETDATE(),
                        IsRead BIT DEFAULT 0,
                        FOREIGN KEY (User_ID)
                            REFERENCES dbo.Users(User_ID)
                    );
                END");

            // LOGIN RECORDS TABLE
            ExecuteNonQuery(connection, @"
                IF OBJECT_ID('dbo.LoginRecords', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.LoginRecords (
                        LoginRecord_ID INT PRIMARY KEY IDENTITY(1,1),
                        User_ID INT NOT NULL,
                        Name NVARCHAR(50) NOT NULL,
                        Surname NVARCHAR(50),
                        Initials NVARCHAR(10),
                        LoginTime DATETIME DEFAULT GETDATE(),
                        Location NVARCHAR(100),
                        FOREIGN KEY (User_ID)
                            REFERENCES dbo.Users(User_ID)
                    );
                END");

            Console.WriteLine("All database tables checked.");
        }

        // ============================================================
        // SEED INITIAL DATA
        // ============================================================

        private void SeedInitialData()
        {
            // ------------------------------------------------------------
            // STEP 1: Seed the Employees table FIRST.
            // Every employee record is fully populated here.
            // ------------------------------------------------------------
            SeedEmployees();

            // ------------------------------------------------------------
            // STEP 2: Seed the Users table, linking each user to an
            // existing Employees row via Employee_ID.
            // ------------------------------------------------------------
            SeedSingleAccount(
                fullName: "Keoagile Mafora",
                username: "keo.admin",
                email: "admin@rocla.com",
                password: "admin123",
                role: "Admin",
                employeeCode: "EMP-ADMIN-001",
                idNumber: "ADMIN-0001");

            SeedSingleAccount(
                fullName: "Mulweli Mbedzi",
                username: "mulweli.super",
                email: "supervisor@rocla.com",
                password: "super123",
                role: "Supervisor",
                employeeCode: "EMP-SUP-001",
                idNumber: "SUP-0001");

            SeedEmployeeUsers();

            Console.WriteLine("Initial account seeding completed.");
        }

        // ============================================================
        // SEED EMPLOYEES TABLE  (explicit, full insert)
        // ============================================================

        private void SeedEmployees()
        {
            // Every employee who will get a user account is declared here,
            // fully populated, BEFORE any Users row is created.
            var employees = new[]
            {
                // Admins / supervisors
                new EmployeeSeed("Keoagile",  "Mafora",     "ADMIN-0001",   "0000000001", "Administrator",       "Administration",  new DateTime(2020, 1, 15), "EMP-ADMIN-001"),
                new EmployeeSeed("Mulweli",   "Mbedzi",     "SUP-0001",     "0000000002", "Supervisor",          "Manufacturing",   new DateTime(2021, 3, 1),  "EMP-SUP-001"),

                // The 20 seeded employees
                new EmployeeSeed("Thabo",      "Mokoena",     "9001015800081", "0710000008", "Software Developer",     "IT",              new DateTime(2022, 2, 7),  "EMP-2026-008"),
                new EmployeeSeed("Lerato",     "Nkosi",       "9002025800082", "0710000009", "HR Manager",             "Human Resources", new DateTime(2022, 3, 14), "EMP-2026-009"),
                new EmployeeSeed("Sipho",      "Dlamini",     "9003035800083", "0710000010", "Accountant",             "Finance",         new DateTime(2022, 4, 4),  "EMP-2026-010"),
                new EmployeeSeed("Naledi",     "Molefe",      "9004045800084", "0710000011", "Marketing Officer",      "Marketing",       new DateTime(2022, 5, 9),  "EMP-2026-011"),
                new EmployeeSeed("Kagiso",     "Pillay",      "9005055800085", "0710000012", "System Administrator",   "IT",              new DateTime(2022, 6, 13), "EMP-2026-012"),
                new EmployeeSeed("Amogelang",  "Ndlovu",      "9006065800086", "0710000013", "Project Manager",        "IT",              new DateTime(2022, 7, 18), "EMP-2026-013"),
                new EmployeeSeed("Karabo",     "Mahlangu",    "9007075800087", "0710000014", "Software Tester",        "IT",              new DateTime(2022, 8, 22), "EMP-2026-014"),
                new EmployeeSeed("Tshepo",     "Khumalo",     "9008085800088", "0710000015", "Sales Representative",   "Sales",           new DateTime(2022, 9, 5),  "EMP-2026-015"),
                new EmployeeSeed("Boitumelo",  "Maseko",      "9009095800089", "0710000016", "Receptionist",           "Administration",  new DateTime(2022, 10, 10),"EMP-2026-016"),
                new EmployeeSeed("Mpho",       "Zwane",       "9010105800090", "0710000017", "Database Administrator", "IT",              new DateTime(2022, 11, 14),"EMP-2026-017"),
                new EmployeeSeed("Refilwe",    "Modise",      "9011115800091", "0710000018", "Payroll Officer",        "Finance",         new DateTime(2023, 1, 9),  "EMP-2026-018"),
                new EmployeeSeed("Tumelo",     "Sithole",     "9012125800092", "0710000019", "Network Engineer",       "IT",              new DateTime(2023, 2, 13), "EMP-2026-019"),
                new EmployeeSeed("Palesa",     "Radebe",      "9101015800093", "0710000020", "Content Creator",        "Marketing",       new DateTime(2023, 3, 20), "EMP-2026-020"),
                new EmployeeSeed("Bongani",    "Cele",        "9102025800094", "0710000021", "Operations Manager",     "Operations",      new DateTime(2023, 4, 17), "EMP-2026-021"),
                new EmployeeSeed("Dineo",      "Mthembu",     "9103035800095", "0710000022", "HR Officer",             "Human Resources", new DateTime(2023, 5, 22), "EMP-2026-022"),
                new EmployeeSeed("Sibusiso",   "Shabalala",   "9104045800096", "0710000023", "Support Technician",     "IT",              new DateTime(2023, 6, 26), "EMP-2026-023"),
                new EmployeeSeed("Zanele",     "Buthelezi",   "9105055800097", "0710000024", "Financial Analyst",      "Finance",         new DateTime(2023, 7, 31), "EMP-2026-024"),
                new EmployeeSeed("Ofentse",    "Mokoena",     "9106065800098", "0710000025", "Business Analyst",       "Operations",      new DateTime(2023, 8, 7),  "EMP-2026-025"),
                new EmployeeSeed("Nokuthula",  "Vilakazi",    "9107075800099", "0710000026", "UI UX Designer",         "Marketing",       new DateTime(2023, 9, 11), "EMP-2026-026"),
                new EmployeeSeed("Katlego",    "Mabena",      "9108085800100", "0710000027", "Junior Developer",       "IT",              new DateTime(2023, 10, 16),"EMP-2026-027"),
            };

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            foreach (var e in employees)
            {
                // Insert only if the ID_Number is not already present.
                using var cmd = new SqlCommand(@"
                    IF NOT EXISTS (
                        SELECT 1 FROM dbo.Employees WHERE ID_Number = @ID_Number
                    )
                    BEGIN
                        INSERT INTO dbo.Employees
                        (
                            First_Name, Last_Name, ID_Number,
                            Phone_Number, Position, Department,
                            Date_Hired, QRCode_Value, IsActive
                        )
                        VALUES
                        (
                            @First_Name, @Last_Name, @ID_Number,
                            @Phone_Number, @Position, @Department,
                            @Date_Hired, @QRCode_Value, 1
                        );
                    END",
                    connection);

                cmd.Parameters.AddWithValue("@First_Name", e.FirstName);
                cmd.Parameters.AddWithValue("@Last_Name", e.LastName);
                cmd.Parameters.AddWithValue("@ID_Number", e.IdNumber);
                cmd.Parameters.AddWithValue("@Phone_Number", (object)e.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Position", (object)e.Position ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Department", (object)e.Department ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Date_Hired", e.DateHired);
                cmd.Parameters.AddWithValue("@QRCode_Value", "QR-" + e.EmployeeCode);

                cmd.ExecuteNonQuery();
            }

            Console.WriteLine($"{employees.Length} employee records checked/seeded.");
        }

        // ============================================================
        // SEED USERS  (linked to Employees via Employee_ID)
        // ============================================================

        private void SeedSingleAccount(
            string fullName,
            string username,
            string email,
            string password,
            string role,
            string employeeCode,
            string idNumber)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                // 1) Already exists?
                using (var checkCommand = new SqlCommand(@"
                    SELECT COUNT(*)
                    FROM dbo.Users
                    WHERE Username = @Username
                       OR Email = @Email
                       OR EmployeeId = @EmployeeId;",
                    connection, transaction))
                {
                    checkCommand.Parameters.AddWithValue("@Username", username);
                    checkCommand.Parameters.AddWithValue("@Email", email);
                    checkCommand.Parameters.AddWithValue("@EmployeeId", employeeCode);

                    if (Convert.ToInt32(checkCommand.ExecuteScalar()) > 0)
                    {
                        transaction.Commit();
                        return;
                    }
                }

                // 2) Look up the Employees row we seeded earlier.
                int employeeId = GetEmployeeIdByCode(connection, transaction, idNumber);

                // 3) Hash password.
                string salt = PasswordHelper.GenerateSalt();
                string hash = PasswordHelper.HashPassword(password, salt);

                // 4) Insert the Users row linked to the employee.
                using var insertCommand = new SqlCommand(@"
                    INSERT INTO dbo.Users
                    (
                        FullName, Username, Email,
                        PasswordHash, Salt, Role,
                        EmployeeId, Employee_ID, Department, Position,
                        IsActive, AccountStatus
                    )
                    SELECT
                        @FullName, @Username, @Email,
                        @PasswordHash, @Salt, @Role,
                        @EmployeeId, e.Employee_ID, e.Department, e.Position,
                        1, 'Active'
                    FROM dbo.Employees e
                    WHERE e.Employee_ID = @Employee_ID;",
                    connection, transaction);

                insertCommand.Parameters.AddWithValue("@FullName", fullName);
                insertCommand.Parameters.AddWithValue("@Username", username);
                insertCommand.Parameters.AddWithValue("@Email", email);
                insertCommand.Parameters.AddWithValue("@PasswordHash", hash);
                insertCommand.Parameters.AddWithValue("@Salt", salt);
                insertCommand.Parameters.AddWithValue("@Role", role);
                insertCommand.Parameters.AddWithValue("@EmployeeId", employeeCode);
                insertCommand.Parameters.AddWithValue("@Employee_ID", employeeId);

                insertCommand.ExecuteNonQuery();
                transaction.Commit();
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                transaction.Rollback();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private void SeedEmployeeUsers()
        {
            // Maps a user account to an existing Employees row (by ID_Number).
            var users = new[]
            {
                new UserSeed("Thabo Mokoena",      "thabo.mokoena",      "thabo.mokoena@example.com",      "9001015800081", "EMP-2026-008"),
                new UserSeed("Lerato Nkosi",       "lerato.nkosi",       "lerato.nkosi@example.com",       "9002025800082", "EMP-2026-009"),
                new UserSeed("Sipho Dlamini",      "sipho.dlamini",      "sipho.dlamini@example.com",      "9003035800083", "EMP-2026-010"),
                new UserSeed("Naledi Molefe",      "naledi.molefe",      "naledi.molefe@example.com",      "9004045800084", "EMP-2026-011"),
                new UserSeed("Kagiso Pillay",      "kagiso.pillay",      "kagiso.pillay@example.com",      "9005055800085", "EMP-2026-012"),
                new UserSeed("Amogelang Ndlovu",   "amogelang.ndlovu",   "amogelang.ndlovu@example.com",   "9006065800086", "EMP-2026-013"),
                new UserSeed("Karabo Mahlangu",    "karabo.mahlangu",    "karabo.mahlangu@example.com",    "9007075800087", "EMP-2026-014"),
                new UserSeed("Tshepo Khumalo",     "tshepo.khumalo",     "tshepo.khumalo@example.com",     "9008085800088", "EMP-2026-015"),
                new UserSeed("Boitumelo Maseko",   "boitumelo.maseko",   "boitumelo.maseko@example.com",   "9009095800089", "EMP-2026-016"),
                new UserSeed("Mpho Zwane",         "mpho.zwane",         "mpho.zwane@example.com",         "9010105800090", "EMP-2026-017"),
                new UserSeed("Refilwe Modise",     "refilwe.modise",     "refilwe.modise@example.com",     "9011115800091", "EMP-2026-018"),
                new UserSeed("Tumelo Sithole",     "tumelo.sithole",     "tumelo.sithole@example.com",     "9012125800092", "EMP-2026-019"),
                new UserSeed("Palesa Radebe",      "palesa.radebe",      "palesa.radebe@example.com",      "9101015800093", "EMP-2026-020"),
                new UserSeed("Bongani Cele",       "bongani.cele",       "bongani.cele@example.com",       "9102025800094", "EMP-2026-021"),
                new UserSeed("Dineo Mthembu",      "dineo.mthembu",      "dineo.mthembu@example.com",      "9103035800095", "EMP-2026-022"),
                new UserSeed("Sibusiso Shabalala", "sibusiso.shabalala", "sibusiso.shabalala@example.com", "9104045800096", "EMP-2026-023"),
                new UserSeed("Zanele Buthelezi",   "zanele.buthelezi",   "zanele.buthelezi@example.com",   "9105055800097", "EMP-2026-024"),
                new UserSeed("Ofentse Mokoena",    "ofentse.mokoena",    "ofentse.mokoena@example.com",    "9106065800098", "EMP-2026-025"),
                new UserSeed("Nokuthula Vilakazi", "nokuthula.vilakazi", "nokuthula.vilakazi@example.com", "9107075800099", "EMP-2026-026"),
                new UserSeed("Katlego Mabena",     "katlego.mabena",     "katlego.mabena@example.com",     "9108085800100", "EMP-2026-027"),
            };

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            foreach (var u in users)
            {
                using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

                try
                {
                    using (var checkCommand = new SqlCommand(@"
                        SELECT COUNT(*)
                        FROM dbo.Users
                        WHERE Username = @Username
                           OR Email = @Email
                           OR EmployeeId = @EmployeeId;",
                        connection, transaction))
                    {
                        checkCommand.Parameters.AddWithValue("@Username", u.Username);
                        checkCommand.Parameters.AddWithValue("@Email", u.Email);
                        checkCommand.Parameters.AddWithValue("@EmployeeId", u.EmployeeCode);

                        if (Convert.ToInt32(checkCommand.ExecuteScalar()) > 0)
                        {
                            transaction.Commit();
                            continue;
                        }
                    }

                    // Look up the Employees row seeded earlier.
                    int employeeId = GetEmployeeIdByCode(connection, transaction, u.IdNumber);
                    if (employeeId <= 0)
                    {
                        // Employees row is missing — skip; SeedEmployees() should have created it.
                        Console.WriteLine($"WARNING: no Employees row for ID_Number {u.IdNumber}; skipping user {u.Username}.");
                        transaction.Rollback();
                        continue;
                    }

                    string salt = PasswordHelper.GenerateSalt();
                    string hash = PasswordHelper.HashPassword("Password@2", salt);

                    using var insertCommand = new SqlCommand(@"
                        INSERT INTO dbo.Users
                        (
                            FullName, Username, Email,
                            PasswordHash, Salt, Role,
                            EmployeeId, Employee_ID, Department, Position,
                            IsActive, AccountStatus
                        )
                        SELECT
                            @FullName, @Username, @Email,
                            @PasswordHash, @Salt, 'Employee',
                            @EmployeeId, e.Employee_ID, e.Department, e.Position,
                            1, 'Active'
                        FROM dbo.Employees e
                        WHERE e.Employee_ID = @Employee_ID;",
                        connection, transaction);

                    insertCommand.Parameters.AddWithValue("@FullName", u.FullName);
                    insertCommand.Parameters.AddWithValue("@Username", u.Username);
                    insertCommand.Parameters.AddWithValue("@Email", u.Email);
                    insertCommand.Parameters.AddWithValue("@PasswordHash", hash);
                    insertCommand.Parameters.AddWithValue("@Salt", salt);
                    insertCommand.Parameters.AddWithValue("@EmployeeId", u.EmployeeCode);
                    insertCommand.Parameters.AddWithValue("@Employee_ID", employeeId);

                    insertCommand.ExecuteNonQuery();
                    transaction.Commit();
                }
                catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
                {
                    transaction.Rollback();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }

            Console.WriteLine("Employee accounts checked/seeded.");
        }

        /// <summary>
        /// Looks up Employees.Employee_ID by ID_Number. Returns 0 if not found.
        /// </summary>
        private int GetEmployeeIdByCode(
            SqlConnection connection,
            SqlTransaction transaction,
            string idNumber)
        {
            using var cmd = new SqlCommand(@"
                SELECT Employee_ID
                FROM dbo.Employees
                WHERE ID_Number = @ID_Number;",
                connection, transaction);

            cmd.Parameters.AddWithValue("@ID_Number", idNumber);

            object result = cmd.ExecuteScalar();
            return result == null || result == DBNull.Value
                ? 0
                : Convert.ToInt32(result);
        }

        // ============================================================
        // DATABASE HELPER METHODS
        // ============================================================

        private void ExecuteNonQuery(SqlConnection connection, string sql)
        {
            using var command = new SqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        public int ExecuteNonQuery(string sql, SqlParameter[] parameters = null)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(sql, connection);
            if (parameters != null)
                command.Parameters.AddRange(parameters);

            return command.ExecuteNonQuery();
        }

        public SqlDataReader ExecuteReader(string sql, SqlParameter[] parameters = null)
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand(sql, connection);
            if (parameters != null)
                command.Parameters.AddRange(parameters);

            try
            {
                return command.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch
            {
                command.Dispose();
                connection.Dispose();
                throw;
            }
        }

        public object ExecuteScalar(string sql, SqlParameter[] parameters = null)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(sql, connection);
            if (parameters != null)
                command.Parameters.AddRange(parameters);

            return command.ExecuteScalar();
        }
    }

    // ================================================================
    // SEED DATA CONTRACTS
    // ================================================================

    internal record EmployeeSeed(
        string FirstName,
        string LastName,
        string IdNumber,
        string Phone,
        string Position,
        string Department,
        DateTime DateHired,
        string EmployeeCode);

    internal record UserSeed(
        string FullName,
        string Username,
        string Email,
        string IdNumber,
        string EmployeeCode);

    // ================================================================
    // PASSWORD HELPER
    // ================================================================

    public static class PasswordHelper
    {
        private const string Pbkdf2Prefix = "PBKDF2$";
        private const int Pbkdf2Iterations = 100_000;
        private const int Pbkdf2SaltBytes = 16;
        private const int Pbkdf2HashBytes = 32;

        public static string GenerateSalt()
        {
            byte[] saltBytes = RandomNumberGenerator.GetBytes(Pbkdf2SaltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        public static string HashPassword(string password, string salt)
        {
            byte[] saltBytes = string.IsNullOrEmpty(salt)
                ? RandomNumberGenerator.GetBytes(Pbkdf2SaltBytes)
                : Convert.FromBase64String(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(
                password, saltBytes, Pbkdf2Iterations, HashAlgorithmName.SHA256);

            byte[] hash = pbkdf2.GetBytes(Pbkdf2HashBytes);

            return $"{Pbkdf2Prefix}{Pbkdf2Iterations}${Convert.ToBase64String(saltBytes)}${Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string password, string salt, string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return false;

            if (hash.StartsWith(Pbkdf2Prefix, StringComparison.Ordinal))
            {
                string[] parts = hash.Split('$');
                if (parts.Length != 4) return false;
                if (!int.TryParse(parts[1], out int iterations)) return false;

                byte[] saltBytes, expected;
                try
                {
                    saltBytes = Convert.FromBase64String(parts[2]);
                    expected = Convert.FromBase64String(parts[3]);
                }
                catch (FormatException) { return false; }

                using var pbkdf2 = new Rfc2898DeriveBytes(
                    password, saltBytes, iterations, HashAlgorithmName.SHA256);
                byte[] actual = pbkdf2.GetBytes(expected.Length);

                return CryptographicOperations.FixedTimeEquals(expected, actual);
            }

            // Legacy SHA-256(password + salt) support.
            try
            {
                using var sha256 = SHA256.Create();
                byte[] bytes = Encoding.UTF8.GetBytes(password + salt);
                byte[] computed = sha256.ComputeHash(bytes);
                byte[] expected = Convert.FromBase64String(hash);
                return CryptographicOperations.FixedTimeEquals(expected, computed);
            }
            catch (FormatException) { return false; }
        }
    }
}