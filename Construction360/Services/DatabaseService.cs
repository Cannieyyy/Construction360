
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Construction360.Services
{
    public class DatabaseService
    {
        public string ConnectionString { get; }
        private readonly string instanceName = "ConstructSystem";

        public DatabaseService()
        {
            ConnectionString = $@"Server=(localdb)\{instanceName};Database=ConstructSystem;Trusted_Connection=true;TrustServerCertificate=true;";

            // Create LocalDB instance first, then initialize database
            CreateClaimSystemInstance();
            InitializeDatabase();
        }

        //Handling the local db
        private void CreateClaimSystemInstance()
        {
            if (CheckInstanceExists())
            {
                Console.WriteLine($"LocalDB instance '{instanceName}' already exists.");
                return;
            }

            try
            {
                //The information about the instance
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c sqllocaldb create \"{instanceName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                //Starting the creation of the local database
                using (var process = new Process { StartInfo = processStartInfo })
                {
                    Console.WriteLine($"Creating LocalDB instance '{instanceName}'...");
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if(process.ExitCode == 0)
                        Console.WriteLine($"LocalDB instance of '{instanceName}' is created successfully");
                    else
                    {
                        Console.WriteLine($"Error creating instance: {error}");
                        throw new Exception($"Failed to create LocalDB instance: {error}");
                    }
                }

                //Starting the instance after creating it
                StartInstance();
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 2)
            {
                throw new Exception("SQL Server LocalDB is not installed. Please install SQL Server Express LocalDB from Microsoft.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create LocalDB instance: {ex.Message}");
            }
        }

        //Method to start the instance
        private void StartInstance()
        {
            try
            {
                //The information about the instance
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c sqllocaldb start \"{instanceName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                //Starting the localDB
                using (var process = new Process {  StartInfo = processStartInfo })
                {
                    Console.WriteLine($"Starting the localDB instance '{instanceName}'...");
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if( process.ExitCode == 0)
                        Console.WriteLine($"LocalDB instance '{instanceName}' has successfully started");
                    else if (!error.Contains("is already running", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"Warning: Could not start instance: {error}");
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Warning: Could not start instance: {ex.Message}");
            }
        }

        //Method to check if the instance exists
        private bool CheckInstanceExists()
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c sqllocaldb info \"{instanceName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = new Process { StartInfo = processStartInfo })
                {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    // If there's an error containing "doesn't exist", instance doesn't exist
                    if (!string.IsNullOrWhiteSpace(error) &&
                        error.Contains($"LocalDB instance \"{instanceName}\" doesn't exist", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    // If we get output and no "doesn't exist" error, instance exists
                    return !string.IsNullOrWhiteSpace(output)
                        && !output.Contains("doesn't exist", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch( Exception ex ) 
            {
                Console.WriteLine($"Error checking instance existence: {ex.Message}");
                return false;
            }
        }

        private void InitializeDatabase()
        {
            try
            {
                // First, create database if it doesn't exist
                CreateDatabase();

                // Then create tables and seed data
                CreateTablesAndSeedData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database initialization error: {ex.Message}");
                throw;
            }
        }

        

        private void CreateDatabase()
        {
            // Use master database connection string for initial creation
            var masterConnectionString = $@"Server=(localdb)\{instanceName};Database=master;Trusted_Connection=true;TrustServerCertificate=true;";

            using (var connection = new SqlConnection(masterConnectionString))
            {
                try
                {
                    connection.Open();

                    string createDbSql = @"
                        IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = 'ConstructSystem')
                        BEGIN
                            CREATE DATABASE ConstructSystem;
                        END";

                    using (var command = new SqlCommand(createDbSql, connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine($"Database '{instanceName}' verified/created successfully!");
                    }
                }
                catch (SqlException ex) when (ex.Number == 4060) // Database doesn't exist
                {
                    // This shouldn't happen due to our check, but handle it anyway
                    Console.WriteLine("Database connection failed. Retrying...");
                    Thread.Sleep(2000); // Wait 2 seconds
                    CreateDatabase(); // Recursive retry
                }
            }
        }
        private void CreateTablesAndSeedData()
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                //Creating the Employees table
                string createEmployeeTable = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'Employees' and xtype = 'U')
                BEGIN
                    CREATE TABLE Employees (
                        Employee_ID INT PRIMARY KEY IDENTITY(1,1),
                        First_Name VARCHAR(50) NOT NULL,
                        Last_Name VARCHAR(50) NOT NULL,
                        ID_Number VARCHAR(20) UNIQUE NOT NULL,
                        Phone_Number VARCHAR(20),
                        Position VARCHAR(50),
                        Department VARCHAR(50),
                        Date_Hired DATE,
                        QRCode_Value VARCHAR(255) UNIQUE
                    )
                END";

                //Creating the Users table (using [Users] instead of 'User' which is a reserved keyword)
                string createUserTable = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'Users' AND xtype = 'U')
                BEGIN
                    CREATE TABLE Users (
                        User_ID INT PRIMARY KEY IDENTITY(1,1),
                        Username VARCHAR(50) UNIQUE NOT NULL,
                        PasswordHash VARCHAR(255) NOT NULL,
                        Role VARCHAR(50) NOT NULL,
                        Employee_ID INT UNIQUE,
                        FOREIGN KEY (Employee_ID) REFERENCES Employees(Employee_ID)
                    )
                END";

                //Creating the Notifications table
                string createNotificationsTable = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'Notifications' and xtype='U')
                BEGIN
                    CREATE TABLE Notifications (
                        Notification_ID INT PRIMARY KEY IDENTITY(1,1),
                        Title VARCHAR(100) NOT NULL,
                        Message TEXT NOT NULL,
                        DateSent DATETIME DEFAULT GETDATE(),
                        SentBy INT,
                        Audience VARCHAR(50),
                        FOREIGN KEY (SentBy) REFERENCES Users(User_ID)
                    )
                END";

                //Creating the Attendance table
                string createAttendanceTable = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'Attendance' and xtype = 'U')
                BEGIN
                    CREATE TABLE Attendance (
                        Attendance_ID INT PRIMARY KEY IDENTITY(1,1),
                        Employee_ID INT NOT NULL,
                        Date DATE NOT NULL,
                        CheckInTime DATETIME,
                        CheckOutTime DATETIME,
                        Status VARCHAR(20),
                        FOREIGN KEY (Employee_ID) REFERENCES Employees(Employee_ID)
                    )
                END";

                //Creating a Productivity table
                string createProductivityTable = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'Productivity' and xtype = 'U')
                BEGIN
                    CREATE TABLE Productivity (
                        Productivity_ID INT PRIMARY KEY IDENTITY(1,1),
                        Employee_ID INT NOT NULL,
                        Date DATE NOT NULL,
                        ToiletsProduced INT DEFAULT 0,
                        DailyTarget INT,
                        RecordedBy INT,
                        FOREIGN KEY (Employee_ID) REFERENCES Employees(Employee_ID),
                        FOREIGN KEY (RecordedBy) REFERENCES Users(User_ID)
                    )
                END";

                
                    // Executing the Employee table first 
                    using (var command = new SqlCommand(createEmployeeTable, connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Employees table is created");
                    }

                    // Executing the Users table 
                    using (var command = new SqlCommand(createUserTable, connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Users table is created");
                    }

                    // Executing the Notification table
                    using (var command = new SqlCommand(createNotificationsTable, connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Notifications table is created");
                    }

                    // Executing the Attendance table 
                    using (var command = new SqlCommand(createAttendanceTable, connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Attendance table is created");
                    }

                    // Executing the Productivity table 
                    using (var command = new SqlCommand(createProductivityTable, connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Productivity table is created");
                    }
                }
                
            }
        
    }
}

