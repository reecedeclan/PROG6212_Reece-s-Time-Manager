# prog6212-poe-reecedeclan
prog6212-poe-reecedeclan created by GitHub Classroom

Reece's Time Manager - Part 3 Readme

Welcome to Reece's Time Manager Part 3(Final)! This iteration of the application builds on the functionalities from Part 2 by introducing a web-based platform, 
enhancing the user interface, and incorporating additional features for an improved study management experience.

Overview

Reece's Time Manager is a web application designed to help you efficiently manage your study time across modules and semesters. This version brings:

User Authentication: Register and log in to securely store your study data.
Semester and Module Management: Define semester duration, add modules, and track study hours.
Enhanced User Interface: Improved navigation and real-time data updates for a seamless user experience.
Detailed Module View: Access additional information for each module, providing comprehensive insights.
Study Hour Logging: Streamlined process for logging study hours with enhanced functionality.
Application Setup

To run Reece's Time Manager Part 3, follow these steps:

You need to clone the Repository:
bash
Copy code
git clone https://github.com/yourusername/prog6212-part-3-reecedeclan.git
cd prog6212-part-3-reecedeclan

Set Up the Environment:
Ensure you have Node.js and npm installed on your machine.

Install Dependencies:
bash
Copy code
npm install
Configure Database Connection:
Update the connection details in the appsettings.json file.

Run the Application:
bash

Copy code: npm start

Access the Application:
Open your web browser and navigate to http://localhost:3000 to start using Reece's Time Manager Part 3.
Using the Application

The core workflow remains similar to the previous version:

Register a New Account:
Click the "Register" link on the login screen.
Enter your name, username, password, and confirm the password.
Click "Register."

Login:
Enter your registered username and password.
Click "Login."

Add a Semester:
Click the "Add New Semester" button.
Enter the number of weeks and start date.
Click "Create the Semester."

Add a Module:
Click the "Add a New Module" button.
Fill in the module code, name, credits, and hours per week.
Click "Add Module to Database."

Log Hours Studied:
Click the "Log Study Hours" button.
Select the module, enter hours studied and the date.
Click "Save."

View Modules:
Click the "View Modules" button.
Click the "Press to Refresh" button to load the latest data.


Application Design

The solution is structured into the following projects:
PROG6212_ST10043367_POEPart3.Data: Class library for database access.
PROG6212_ST10043367_POEPart3.Domain: Class library containing domain models and business logic.
PROG6212_ST10043367_POEPart3.WebUI: ASP.NET Core web application with enhanced user interface.
The web application uses Entity Framework Core for SQL Server database access, incorporating ASP.NET Core Identity for authentication.

The layered architecture ensures a separation of concerns, providing a scalable and maintainable solution for study time management.

Support
If you encounter any issues or have questions, feel free to reach out to me at st10043367@vcconnect.edu.za
