using System;
using System.Collections.Generic;

namespace RestaurantManagementSystem.ConsoleDemo
{
    public class DashboardChartDemo
    {
        public static void ShowChartDataRepresentation()
        {
            Console.WriteLine("\nSales Data Chart (ASCII representation):");
            Console.WriteLine("--------------------------------------");
            
            var salesData = GetSampleSalesData();
            int maxAmount = 0;
            
            // Find the maximum amount for scaling
            foreach (var data in salesData)
            {
                if ((int)data.Amount > maxAmount)
                {
                    maxAmount = (int)data.Amount;
                }
            }
            
            // Calculate the scale (10 characters height)
            double scale = 10.0 / maxAmount;
            
            // Draw the chart from top to bottom
            for (int row = 10; row >= 0; row--)
            {
                Console.Write($"{(row * maxAmount / 10),4} |");
                
                foreach (var data in salesData)
                {
                    int barHeight = (int)((double)data.Amount * scale);
                    Console.Write(barHeight >= row ? " █ " : "   ");
                }
                
                Console.WriteLine();
            }
            
            // Draw the x-axis
            Console.Write("     +");
            for (int i = 0; i < salesData.Count; i++)
            {
                Console.Write("---");
            }
            Console.WriteLine();
            
            // Draw the days
            Console.Write("      ");
            foreach (var data in salesData)
            {
                Console.Write($"{data.Day[0]} ");
            }
            Console.WriteLine("\n");
            
            // Customer traffic by hour chart
            Console.WriteLine("Customer Traffic by Hour (ASCII representation):");
            Console.WriteLine("----------------------------------------------");
            
            var customerData = GetSampleCustomerData();
            int maxCustomers = 0;
            
            // Find maximum customers for scaling
            foreach (var data in customerData)
            {
                if (data.CustomerCount > maxCustomers)
                {
                    maxCustomers = data.CustomerCount;
                }
            }
            
            // Calculate the scale (8 characters height)
            scale = 8.0 / maxCustomers;
            
            // Draw the chart from top to bottom
            for (int row = 8; row >= 0; row--)
            {
                Console.Write($"{(row * maxCustomers / 8),3} |");
                
                foreach (var data in customerData)
                {
                    int barHeight = (int)(data.CustomerCount * scale);
                    Console.Write(barHeight >= row ? " █" : "  ");
                }
                
                Console.WriteLine();
            }
            
            // Draw the x-axis
            Console.Write("    +");
            for (int i = 0; i < customerData.Count; i++)
            {
                Console.Write("--");
            }
            Console.WriteLine();
            
            // Draw the hours
            Console.Write("     ");
            foreach (var data in customerData)
            {
                Console.Write($"{data.Hour}");
                if (data.Hour < 10) Console.Write(" ");
            }
            Console.WriteLine();
        }
        
        static List<SalesDataViewModel> GetSampleSalesData()
        {
            return new List<SalesDataViewModel>
            {
                new SalesDataViewModel { Day = "Monday", Amount = 3200 },
                new SalesDataViewModel { Day = "Tuesday", Amount = 2800 },
                new SalesDataViewModel { Day = "Wednesday", Amount = 4100 },
                new SalesDataViewModel { Day = "Thursday", Amount = 3600 },
                new SalesDataViewModel { Day = "Friday", Amount = 4800 },
                new SalesDataViewModel { Day = "Saturday", Amount = 5200 },
                new SalesDataViewModel { Day = "Sunday", Amount = 4890 }
            };
        }
        
        static List<CustomersByTimeViewModel> GetSampleCustomerData()
        {
            return new List<CustomersByTimeViewModel>
            {
                new CustomersByTimeViewModel { Hour = 11, CustomerCount = 15 },
                new CustomersByTimeViewModel { Hour = 12, CustomerCount = 28 },
                new CustomersByTimeViewModel { Hour = 13, CustomerCount = 32 },
                new CustomersByTimeViewModel { Hour = 14, CustomerCount = 18 },
                new CustomersByTimeViewModel { Hour = 15, CustomerCount = 12 },
                new CustomersByTimeViewModel { Hour = 17, CustomerCount = 22 },
                new CustomersByTimeViewModel { Hour = 18, CustomerCount = 38 },
                new CustomersByTimeViewModel { Hour = 19, CustomerCount = 45 },
                new CustomersByTimeViewModel { Hour = 20, CustomerCount = 36 },
                new CustomersByTimeViewModel { Hour = 21, CustomerCount = 24 }
            };
        }
    }
}
