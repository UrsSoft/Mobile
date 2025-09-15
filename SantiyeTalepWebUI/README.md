# My MVC App

This is a simple ASP.NET MVC application that demonstrates the basic structure and functionality of an MVC project.

## Project Structure

- **Controllers**: Contains the controllers that handle user requests.
  - `HomeController.cs`: Manages requests for the home page.

- **Models**: Contains the data models used in the application.
  - `ErrorViewModel.cs`: Represents error information passed to views.

- **Views**: Contains the Razor views for rendering HTML.
  - **Home**: Contains views related to the home page.
    - `Index.cshtml`: The main view for the home page.
  - **Shared**: Contains shared views and layout.
    - `_Layout.cshtml`: The layout for the application.
    - `Error.cshtml`: Displays error messages.
  - `_ViewStart.cshtml`: Specifies the default layout for views.

- **wwwroot**: Contains static files such as CSS, JavaScript, and libraries.
  - **css**: Contains stylesheets.
    - `site.css`: Styles for the application.
  - **js**: Contains JavaScript files.
    - `site.js`: JavaScript code for interactivity.
  - **lib**: Directory for third-party libraries.

- **appsettings.json**: Configuration settings for the application.

- **Program.cs**: The entry point of the application.

## Setup Instructions

1. Clone the repository or download the project files.
2. Open the project in your preferred IDE.
3. Restore the NuGet packages.
4. Run the application using the command `dotnet run` or through your IDE.

## Usage

Navigate to the home page to see the application in action. You can explore the various features and views provided by the application.

## License

This project is licensed under the MIT License.