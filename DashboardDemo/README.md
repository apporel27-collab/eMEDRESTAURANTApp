# Restaurant Management System Dashboard

## Overview
This console application demonstrates a restaurant management dashboard with sample data visualization capabilities. It shows key metrics for restaurant operations including sales, orders, inventory, and customer traffic.

## Features
- Real-time dashboard statistics
- Order tracking and management
- Inventory monitoring
- Sales and customer traffic visualization
- Data refresh simulation

## Running the Demo
To run the demo application:

```bash
cd DashboardDemo
dotnet run
```

After starting the application, you can:
- View the initial dashboard data
- Press 'R' to refresh the dashboard with updated statistics
- Press Enter to exit the application

## Project Structure

### Main Components
- `Program.cs` - Main application entry point and dashboard display logic
- `dashboard_chart.cs` - ASCII chart visualization for sales and customer data

### Data Models
- `DashboardViewModel` - Core data structure for dashboard statistics
- Various supporting view models for specific data visualization needs:
  - Order information
  - Inventory tracking
  - Menu popularity metrics
  - Sales trends
  - Customer traffic patterns

## Next Steps for Web Integration
To integrate this dashboard into the web application:

1. Transfer the data structures to the ASP.NET Core MVC application
2. Use the sample data generation in HomeController as a fallback when database is unavailable
3. Implement Chart.js for visual representation instead of ASCII charts
4. Add AJAX refresh functionality to update dashboard without full page reloads

## Notes
This demo shows a functional prototype of the dashboard capabilities without database dependencies. The sample data generation methods can be used as fallbacks in the main application when the database is unavailable or during development.
