using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Reflection;

namespace Shylock
{
    public class Repository
    {
        private string _connectionString;

        public Repository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<T> ExecuteSql<T>(string sqlStatement, Dictionary<string, object> parameters = null, bool isStoredProcedure = false)
        {
            if (string.IsNullOrEmpty(sqlStatement))
            {
                throw new ArgumentNullException(nameof(sqlStatement));
            }

            List<T> result = new List<T>();

            FieldInfo[] fields = typeof(T).GetFields();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(sqlStatement, connection))
                {
                    command.CommandType = isStoredProcedure ? System.Data.CommandType.StoredProcedure : System.Data.CommandType.Text;

                    if (parameters != null)
                    {
                        foreach (KeyValuePair<string, object> parameter in parameters)
                        {
                            command.Parameters.Add(new SqlParameter() { ParameterName = parameter.Key, Value = parameter.Value });
                        }
                    }

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            T entity = default(T);

                            foreach (FieldInfo field in fields)
                            {
                                object dbContent = reader[field.Name];

                                Type t = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
                                dbContent = dbContent == null ? null : Convert.ChangeType(dbContent, t);

                                field.SetValue(entity, dbContent);
                            }

                            result.Add(entity);
                        }
                    }

                }
            }

            return result;
        }

        public void ExecuteSql<T>(T entity, string sqlStatement, bool isStoredProcedure = false)
        {
            if (entity == null || EqualityComparer<T>.Default.Equals(entity, default(T)))
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (string.IsNullOrEmpty(sqlStatement))
            {
                throw new ArgumentNullException(nameof(sqlStatement));
            }

            Validator.ValidateObject(entity, new ValidationContext(entity));

            FieldInfo[] fields = typeof(T).GetFields();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(sqlStatement, connection))
                {
                    command.CommandType = isStoredProcedure ? System.Data.CommandType.StoredProcedure : System.Data.CommandType.Text;

                    foreach (FieldInfo field in fields)
                    {
                        command.Parameters.Add(new SqlParameter() { ParameterName = field.Name, Value = field.GetValue(entity) });
                    }

                    command.ExecuteReader();
                }
            }
        }
    }
}
