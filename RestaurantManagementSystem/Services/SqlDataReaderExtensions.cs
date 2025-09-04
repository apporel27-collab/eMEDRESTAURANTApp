using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace RestaurantManagementSystem.Services
{
    public static class SqlDataReaderExtensions
    {
        /// <summary>
        /// Checks if a column exists in the SqlDataReader
        /// </summary>
        /// <param name="reader">The SqlDataReader to check</param>
        /// <param name="columnName">The name of the column to check for</param>
        /// <returns>True if the column exists, false otherwise</returns>
        public static bool HasColumn(this SqlDataReader reader, string columnName)
        {
            if (reader == null || string.IsNullOrEmpty(columnName))
                return false;
            
            try
            {
                // Try to get the ordinal of the column - this will throw if the column doesn't exist
                int ordinal = reader.GetOrdinal(columnName);
                return true;
            }
            catch (IndexOutOfRangeException)
            {
                // Column doesn't exist
                return false;
            }
        }
    }
}
