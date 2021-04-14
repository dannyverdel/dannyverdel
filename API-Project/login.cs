/* ************************************************************************** */
/*  login.cs                                                                  */
/*  By: Danny Verdel <danny.verdel@gmail.com                                  */
/*  Created: 2021/04/14 09:15                                                 */
/*  Updated: 2021/04/14 09:30                                                 */
/*                                                                            */
/*  Let users be able to receive a token to authenticate themselves           */
/*  in other API's                                                            */
/*                                                                            */
/*                                                                            */
/* ************************************************************************** */

using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ApiDobbeTransport
{
    public class login
    {
        //Retrieve connection string
        private static string _connectionString = Environment.GetEnvironmentVariable("ConnectionString");

        //Initialize request variables for the request body
        public class Request
        {
            public int Customer { get; set; }
            public string CustomerName { get; set; }
            public string Password { get; set; }
        }

        //Checking if the user forgot to fill out anything
        public class Nulls
        {
            //Checking if Customer is null
            public static bool Customer(int customer)
            {
                if(customer == 0)
                    return true;
                else
                    return false;
            }

            //Checking if CustomerName is null
            public static bool CustomerName(string customerName)
            {
                if (string.IsNullOrEmpty(customerName))
                    return true;
                else
                    return false;
            }

            //Checking if Password is null
            public static bool Password(string password)
            {
                if (string.IsNullOrEmpty(password))
                    return true;
                else
                    return false;
            }
        }

        /*
        Checking for SQL injections
        User input is compared with a list of unvalid keywords
        For security reasons the list has been left out of the code
        */
        public class SqlInjection
        {
            //Checking for Customer
            public static bool Customer(int customer)
            {
                var lower = customer.ToString().ToLower();
                for (int x = 0; x < _invalid.Length; x++)
                    if (lower.Contains(_invalid[x]))
                        return true;
                    return false;
            }

            //Checking for CustomerName
            public static bool CustomerName(string customerName)
            {
                var lower = customerName.ToString().ToLower();
                for (int x = 0; x < _invalid.Length; x++)
                    if (lower.Contains(_invalid[x]))
                        return true;
                return false;
            }

            //Checking for Password
            public static bool Password(string password)
            {
                var lower = password.ToString().ToLower();
                for (int x = 0; x < _invalid.Length; x++)
                    if (lower.Contains(_invalid[x]))
                        return true;
                return false;
            }
        }

        /*
        Authenticating credentials
        User input is being compared to a database
        */
        public class Authenticate
        {
            public static bool Info(int customer, string customerName, string password)
            {
                var t = 0;

                string query = String.Format("SELECT * FROM dbo.customers WHERE Customer = '" +
                customer + "' AND CustomerName = '" + customerName + "' AND Password = '" + password + "'");

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    SqlCommand command = new SqlCommand(query, conn);
                    conn.Open();
                    SqlDataReader dbread = command.ExecuteReader();

                    while (dbread.Read())
                    {
                        t++;
                    }

                    command.Dispose();
                    dbread.Close();
                    conn.Close();

                    if (t == 1)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        //Creating a random token of 150 characters long and saving it in a table
        public class Create
        {
            //Creating of the token
            public static string Token()
            {
                string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
                var random = new Random();
                var tokenString = new string(Enumerable.Repeat(chars, 150).Select(s => s[random.Next(s.Length)]).ToArray());

                return tokenString;
            }

            //Saving token in the database
            public static void DbToken(int Customer, string CustomerName, string token)
            {
                var now = Convert.ToDateTime(DateTime.Now).ToString("yyyy-MM-dd HH:mm:ss.fff");

                string query = String.Format("UPDATE dbo.customers SET Token = '"
                    + token + "', CreatedToken = '" + now + "' WHERE Customer = '" +
                    Customer + "' AND CustomerName = '" + CustomerName + "'");

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    SqlCommand dbcom = new SqlCommand(query, conn);
                    conn.Open();
                    dbcom.ExecuteNonQuery();
                    dbcom.Dispose();
                    conn.Close();
                }
            }
        }

        //Templates for Http responses
        public class Response
        {
            //OK response template
            public static HttpResponseMessage Ok(string token)
            {
                ResponseJson response = new ResponseJson();
                response.token = token;
                response.ttl = 3600000;
                response.units = "milliseconds";
                string json = JsonConvert.SerializeObject(response);

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            }

            //bad request template
            public static HttpResponseMessage BadRequest(string description)
            {
                ResponseCode response = new ResponseCode();
                response.code = 400;
                response.info = "Bad Request";
                response.description = description;
                string json = JsonConvert.SerializeObject(response);

                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            }

            //Unauthorized response template
            public static HttpResponseMessage Unauthorized(string description)
            {
                ResponseCode response = new ResponseCode();
                response.code = 401;
                response.info = "Unauthorized";
                response.description = description;
                string json = JsonConvert.SerializeObject(response);

                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            }
        }

        //Json response template
        public class ResponseJson
        {
            string _token;
            int _ttl;
            string _units;

            public string token
            {
                get { return _token; }
                set { _token = value; }
            }

            public int ttl
            {
                get { return _ttl; }
                set { _ttl = value; }
            }

            public string units
            {
                get { return _units; }
                set { _units = value; }
            }
        }

        //Response codes template
        public class ResponseCode
        {
            int _code;
            string _info;
            string _description;

            public int code
            {
                get { return _code; }
                set { _code = value; }
            }

            public string info
            {
                get { return _info; }
                set { _info = value; }
            }

            public string description
            {
                get { return _description; }
                set { _description = value; }
            }
        }

        [FunctionName("login")]
        public static async Task<HttpResponseMessage> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            try
            {
                //Retrieving request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                Request request = JsonConvert.DeserializeObject<Request>(requestBody);

                //Saving the request body
                int Customer = request.Customer;
                string CustomerName = request.CustomerName;
                string Password = request.Password;

                //Checking for nulls
                if(Nulls.Customer(Customer))
                    return Response.BadRequest("'Customer' is niet ingevuld");
                else if(Nulls.CustomerName(CustomerName))
                    return Response.BadRequest("'CustomerName' is niet ingevuld");
                else if(Nulls.Password(Password))
                    return Response.BadRequest("'Password' is niet ingevuld");

                //Checking for SQL injections
                if(SqlInjection.Customer(Customer))
                    return Response.BadRequest("'Customer' bevat niet toegestane script");
                else if(SqlInjection.CustomerName(CustomerName))
                    return Response.BadRequest("'CustomerName' bevat niet toegestane script");
                else if(SqlInjection.Password(Password))
                    return Response.BadRequest("'Password' bevat niet toegestane script");

                //Authenticating user credentials
                if(Authenticate.Info(Customer, CustomerName, Password))
                {
                    //Creating, saving and returning token
                    string token = Create.Token();
                    Create.DbToken(Customer, CustomerName, token);
                    return Response.Ok(token);
                }
                else
                {
                    return Response.Unauthorized("Inlog gegevens kloppen niet");
                }
            }
            //Fail safe if the user forgot a comma, bracket, etc.
            catch
            {
                return Response.BadRequest("'Customer', 'CustomerName' of 'Password' is niet goed gespecificeerd");
            }
        }
    }
}
