using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Construction360.Services
{
    public class DatabaseService
    {
        private readonly string _instanceName = "ConstructSystem";
        private readonly string _databaseName = "ConstructSystem";
        public string ConnectionString { get; }

        public DatabaseService()
        {
            // Using consistent naming
            ConnectionString = $@"Server=(localdb)\{_instanceName};Database={_databaseName};Trusted_Connection=true;TrustServerCertificate=true;";

            // Create and initialize database
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                CreateLocalDBInstance();
                CreateDatabase();
                CreateTables();
                SeedInitialData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database initialization error: {ex.Message}");
                throw;
            }
        }

        private void CreateLocalDBInstance()
        {
            if (CheckInstanceExists()) return;

            try
            {
                //The information about the instance
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c sqllocaldb create \"{_instanceName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                //Starting the creation of the local database
                using var process = new Process { StartInfo = startInfo };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine($"LocalDB instance of '{_instanceName}' is created successfully");
                    
                    //Starting the instance after creating the instance
                    StartLocalDBInstance();
                }
                else
                {
                    //When the instance has failed to be created
                    Console.WriteLine($"Error creating instance: {error}");
                    throw new Exception($"Failed to create LocalDB instance: {error}");
                }
            }

            //When the server is not installed
            catch (Win32Exception ex) when (ex.NativeErrorCode == 2)
            {
                throw new Exception("SQL Server LocalDB is not installed. Please install SQL Server Express LocalDB from Microsoft.");
            }
        }

        //Starting the localDB instance
        private void StartLocalDBInstance()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c sqllocaldb start \"{_instanceName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not start instance: {ex.Message}");
            }
        }

        private bool CheckInstanceExists()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c sqllocaldb info \"{_instanceName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return !string.IsNullOrWhiteSpace(output) &&
                       !output.Contains("doesn't exist", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private void CreateDatabase()
        {
            var masterConnectionString = $@"Server=(localdb)\{_instanceName};Database=master;Trusted_Connection=true;TrustServerCertificate=true;";

            using var connection = new SqlConnection(masterConnectionString);
            connection.Open();

            string createDbSql = @"
                IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = @dbName)
                BEGIN
                    CREATE DATABASE ConstructSystem;
                END";

            using var command = new SqlCommand(createDbSql, connection);
            command.Parameters.AddWithValue("@dbName", _databaseName);
            command.ExecuteNonQuery();
        }

        private void CreateTables()
        {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();

            // Users table (with hashed passwords)
            string createUsersTable = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'Users' AND xtype = 'U')
                BEGIN
                    CREATE TABLE Users (
                        User_ID INT PRIMARY KEY IDENTITY(1,1),
                        FullName NVARCHAR(100) NOT NULL,
                        Username NVARCHAR(50) UNIQUE NOT NULL,
                        Email NVARCHAR(100) UNIQUE NOT NULL,
                        PasswordHash NVARCHAR(255) NOT NULL,
                        Salt NVARCHAR(50) NOT NULL,
                        Role NVARCHAR(20) NOT NULL,
                        EmployeeId NVARCHAR(20) UNIQUE,
                        Department NVARCHAR(50),
                        Position NVARCHAR(50),
                        IsActive BIT DEFAULT 1,
                        CreatedDate DATETIME DEFAULT GETDATE(),
                        LastLoginDate DATETIME
                    )
                END";

            // Employees table
            string createEmployeesTable = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'Employees' AND xtype = 'U')
                BEGIN
                    CREATE TABLE Employees (
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
                    )
                END";

            // Attendance table
            string createAttendanceTable = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'Attendance' AND xtype = 'U')
                BEGIN
                    CREATE TABLE Attendance (
                        Attendance_ID INT PRIMARY KEY IDENTITY(1,1),
                        Employee_ID INT NOT NULL,
                        Date DATE NOT NULL,
                        CheckInTime DATETIME,
                        CheckOutTime DATETIME,
                        Status NVARCHAR(20),
                        FOREIGN KEY (Employee_ID) REFERENCES Employees(Employee_ID)
                    )
                END";

            // Leave Requests table
            string createLeaveRequestsTable = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'LeaveRequests' AND xtype = 'U')
                BEGIN
                    CREATE TABLE LeaveRequests (
                        Leave_ID INT PRIMARY KEY IDENTITY(1,1),
                        Employee_ID INT NOT NULL,
                        Type NVARCHAR(20) NOT NULL,
                        Status NVARCHAR(20) NOT NULL,
                        StartDate DATE NOT NULL,
                        EndDate DATE NOT NULL,
                        Reason NVARCHAR(500),
                        SubmittedDate DATETIME DEFAULT GETDATE(),
                        FOREIGN KEY (Employee_ID) REFERENCES Employees(Employee_ID)
                    )
                END";

            // Notifications table
            string createNotificationsTable = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'Notifications' AND xtype = 'U')
                BEGIN
                    CREATE TABLE Notifications (
                        Notification_ID INT PRIMARY KEY IDENTITY(1,1),
                        User_ID INT,
                        Title NVARCHAR(100) NOT NULL,
                        Message NVARCHAR(MAX) NOT NULL,
                        Type NVARCHAR(20),
                        DateSent DATETIME DEFAULT GETDATE(),
                        IsRead BIT DEFAULT 0,
                        FOREIGN KEY (User_ID) REFERENCES Users(User_ID)
                    )
                END";

            // Execute all table creations
            ExecuteNonQuery(connection, createUsersTable);
            Console.WriteLine("Users table has been created");
            ExecuteNonQuery(connection, createEmployeesTable);
            Console.WriteLine("Employee table has been created");
            ExecuteNonQuery(connection, createAttendanceTable);
            Console.WriteLine("Attendance table has been created");
            ExecuteNonQuery(connection, createLeaveRequestsTable);
            Console.WriteLine("Leave request table has been created");
            ExecuteNonQuery(connection, createNotificationsTable);
            Console.WriteLine("Notifications table has been created");
        }

        private void ExecuteNonQuery(SqlConnection connection, string sql)
        {
            using var command = new SqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        private void SeedInitialData()
        {
            // Check if admin user exists
            var adminExists = ExecuteScalar("SELECT COUNT(*) FROM Users WHERE Email = 'admin@rocla.com'");
            if (Convert.ToInt32(adminExists) == 0)
            {
                // Create admin user with hashed password
                var salt = PasswordHelper.GenerateSalt();
                var hashedPassword = PasswordHelper.HashPassword("admin123", salt);

                var sql = @"
                    INSERT INTO Users (FullName, Username, Email, PasswordHash, Salt, Role, EmployeeId, Department, Position, IsActive)
                    VALUES (@FullName, @Username, @Email, @PasswordHash, @Salt, @Role, @EmployeeId, @Department, @Position, 1)";

                using var connection = new SqlConnection(ConnectionString);
                connection.Open();

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@FullName", "Keoagile Mafora");
                command.Parameters.AddWithValue("@Username", "keo.admin");
                command.Parameters.AddWithValue("@Email", "admin@rocla.com");
                command.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                command.Parameters.AddWithValue("@Salt", salt);
                command.Parameters.AddWithValue("@Role", "Admin");
                command.Parameters.AddWithValue("@EmployeeId", "EMP-ADMIN-001");
                command.Parameters.AddWithValue("@Department", "Administration");
                command.Parameters.AddWithValue("@Position", "Administrator");
                command.ExecuteNonQuery();

                // Add supervisor
                var salt2 = PasswordHelper.GenerateSalt();
                var hashedPassword2 = PasswordHelper.HashPassword("super123", salt2);

                var sql2 = @"
                    INSERT INTO Users (FullName, Username, Email, PasswordHash, Salt, Role, EmployeeId, Department, Position, IsActive)
                    VALUES (@FullName, @Username, @Email, @PasswordHash, @Salt, @Role, @EmployeeId, @Department, @Position, 1)";

                using var command2 = new SqlCommand(sql2, connection);
                command2.Parameters.AddWithValue("@FullName", "Mulweli Mbedzi");
                command2.Parameters.AddWithValue("@Username", "mulweli.super");
                command2.Parameters.AddWithValue("@Email", "supervisor@rocla.com");
                command2.Parameters.AddWithValue("@PasswordHash", hashedPassword2);
                command2.Parameters.AddWithValue("@Salt", salt2);
                command2.Parameters.AddWithValue("@Role", "Supervisor");
                command2.Parameters.AddWithValue("@EmployeeId", "EMP-SUP-001");
                command2.Parameters.AddWithValue("@Department", "Manufacturing");
                command2.Parameters.AddWithValue("@Position", "Supervisor");
                command2.ExecuteNonQuery();
            }
        }

        private object ExecuteScalar(string sql)
        {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();
            using var command = new SqlCommand(sql, connection);
            return command.ExecuteScalar();
        }

        // Helper method for executing SQL
        public int ExecuteNonQuery(string sql, SqlParameter[] parameters = null)
        {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();
            using var command = new SqlCommand(sql, connection);
            if (parameters != null)
                command.Parameters.AddRange(parameters);
            return command.ExecuteNonQuery();
        }

        // Helper method for retrieving data
        public SqlDataReader ExecuteReader(string sql, SqlParameter[] parameters = null)
        {
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            using var command = new SqlCommand(sql, connection);
            if (parameters != null)
                command.Parameters.AddRange(parameters);
            return command.ExecuteReader(CommandBehavior.CloseConnection);
        }
    }

    // Password Helper Class
    public static class PasswordHelper
    {
        public static string GenerateSalt()
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            byte[] saltBytes = new byte[16];
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        public static string HashPassword(string password, string salt)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var combined = password + salt;
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(combined);
            byte[] hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public static bool VerifyPassword(string password, string salt, string hash)
        {
            var computedHash = HashPassword(password, salt);
            return computedHash == hash;
        }
    }
}