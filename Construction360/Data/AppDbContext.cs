using System.Data.SqlClient;

namespace Construction360.Data
{
    public class AppDbContext
    {
        private readonly string connectionString;
        public AppDbContext()
        {
            connectionString = @"Server=(localdb)\ConstuctSystem;Trusted_Connection=true;TrustServerCertificate=true;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    //Creating the database if its not there
                    string createDatabase = @"IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = 'ConstructSystem')
                                            BEGIN
                                                CREATE DATABASE ConstructSystem;
                                            END";

                    //Executing the command writen above
                    using (var command = new SqlCommand(createDatabase, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    connection.ChangeDatabase("ConstructSystem");

                    //Creating the Employees table
                    string createEmployeeTable = @"
                        IF NOT EXIST (SELECT * FROM sysobjects WHERE name = 'Employee' and xtype = 'U')
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
                            QRCode_Value VARCHAR(255) UNIQUE)
                        END";

                    //Creating the Users table
                    string createUserTable = @"
                        IF NOT EXIST (SELECT * FROM sysobjects WHERE name = 'User' AND xtype = 'U')
                        BEGIN
                            CREATE TABLE User (
                            User_ID INT PRIMARY KEY IDENTITY(1,1),
                            Username VARCHAR(50) UNIQUE NOT NULL,
                            PasswordHash VARCHAR(255) NOT NULL,
                            Role VARCHAR(50) NOT NULL,
                            Employee_ID INT UNIQUE FOREIGN KEY (Employee_ID) REFERENCES Employees(Employee_ID))
                        END";

                    //Creating the Notifications table
                    string createNotificationsTable = @"
                        IF NOT EXIST (SELECT * FROM sysobjects WHERE name = 'Notifications' and xtype='U')
                        BEGIN
                            CREATE TABLE Notifications (
                            Notification_ID INT PRIMARY KEY IDENTITY(1,1),
                            Title VARCHAR(100) NOT NULL,
                            Message TEXT NOT NULL,
                            DateSent DATETIME DEFAULT GETDATE(),
                            SentBy INT FOREIGN KEY (SentBy) REFERENCES User(User_ID), 
                            Audience VARCHAR(50))
                        END";

                    //Creating the Attendance table
                    string createAttendanceTable = @"
                        IF NOT EXIST (SELECT * FROM sysobjects WHERE name = 'Attendance' and xType = 'U'
                        BEGIN
                            CREATE TABLE Attendance (
                            Attendance_ID INT PRIMARY KEY IDENTITY(1,1),
                            Employee_ID INT NOT NULL FOREIGN KEY (Employee_ID) REFERENCES Employees(Employee_ID),
                            Date DATE NOT NULL,
                            CheckInTime DATETIME,
                            CheckOutTime DATETIME,
                            Status VARCHAR(20))
                        END";

                    //Creating a Productivity table
                    string createProductivityTable = @"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'Productivity' and xtype = 'U'
                        BEGIN
                            CREATE TABLE Productivity (
                            Productivity_ID INT PRIMARY KEY IDENTITY(1,1),
                            Employee_ID INT NOT NULL FOREIGN KEY (Employee_ID) REFERENCES Employees(Employee_ID),
                            Date DATE NOT NULL,
                            ToiletsProduced INT DEFAULT 0,
                            DailyTarget INT,
                            RecordedBy INT FOREIGN KEY (RecordedBy) REFERENCES User(User_ID))
                        END";

                    //Executing the Employee table
                    using (var command = new SqlCommand(createEmployeeTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    //Executing the User table
                    using (var command = new SqlCommand(createUserTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    //Executing the Notification table
                    using (var command = new SqlCommand(createNotificationsTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    //Executing the Attendance table
                    using (var command = new SqlCommand(createAttendanceTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    //Executing the Production table
                    using (var command = new SqlCommand(createProductivityTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database initialization error: {ex.Message}");
                throw;
            }
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString + "Database=ConstuctSystem");
        }
    }
}

