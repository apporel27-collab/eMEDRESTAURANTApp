using System;
using System.Data;

namespace RestaurantManagementSystem.Helpers
{
    public static class DatabaseHelper
    {
        /// <summary>
        /// Safely gets a value from a data reader, returning default if the value is DBNull
        /// </summary>
        public static T GetValueOrDefault<T>(this IDataReader reader, string columnName)
        {
            try 
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.GetValueOrDefault<T>(ordinal);
            }
            catch (IndexOutOfRangeException)
            {
                // If the column doesn't exist, return default value
                return default(T);
            }
        }

        /// <summary>
        /// Safely gets a value from a data reader by ordinal, returning default if the value is DBNull
        /// </summary>
        public static T GetValueOrDefault<T>(this IDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return default(T);
            }

            object value = reader.GetValue(ordinal);
            
            // Handle specific type conversions
            if (typeof(T) == typeof(string))
            {
                return (T)(object)value.ToString();
            }
            else if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
            {
                return (T)(object)Convert.ToInt32(value);
            }
            else if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
            {
                return (T)(object)Convert.ToDecimal(value);
            }
            else if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
            {
                return (T)(object)Convert.ToBoolean(value);
            }
            else if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
            {
                return (T)(object)Convert.ToDateTime(value);
            }
            
            return (T)value;
        }

        /// <summary>
        /// Creates a parameter that properly handles null values
        /// </summary>
        public static object GetDbParameterValue(object value)
        {
            return value ?? DBNull.Value;
        }
    }
}
