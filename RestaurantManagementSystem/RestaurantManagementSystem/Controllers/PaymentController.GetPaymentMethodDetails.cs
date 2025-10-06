// Add a new API endpoint to get payment method details
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace RestaurantManagementSystem.Controllers
{
    public partial class PaymentController
    {
        [HttpGet]
        public IActionResult GetPaymentMethodDetails(int id)
        {
            if (id <= 0)
            {
                return Json(new { requiresCardInfo = false });
            }
            
            using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                    "SELECT RequiresCardInfo, Name, DisplayName FROM PaymentMethods WHERE Id = @Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bool requiresCardInfo = reader.GetBoolean(0);
                            string name = reader.GetString(1);
                            string displayName = reader.GetString(2);
                            
                            // Return payment method details
                            return Json(new { 
                                requiresCardInfo = requiresCardInfo,
                                name = name,
                                displayName = displayName,
                                isComplementary = name.Equals("Complementary", StringComparison.OrdinalIgnoreCase),
                                cardTypes = requiresCardInfo ? new[] { "Visa", "MasterCard", "American Express", "Discover", "Diners Club", "Other" } : null
                            });
                        }
                    }
                }
            }
            
            return Json(new { requiresCardInfo = false });
        }
    }
}