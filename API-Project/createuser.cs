
/* ************************************************************************** */
/*  createuser.cs                                                             */
/*  By: Danny Verdel <danny.verdel@gmail.com>                                 */
/*  Created: 2021/04/11 17:45                                                 */
/*  Updated: 2021/04/14 09:30                                                 */
/*                                                                            */
/*  Let users be able to create a user account by themselves.                 */
/*  They can later use this account to access other APIs                      */
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
    public static class createuser
    {
        //Retrieve connection string
        private static string _connectionString = Environment.GetEnvironmentVariable("ConnectionString");

        //Setting up the request body
        public class Request
        {
            public int RelationCompanynr { get; set; }
            public int Relation { get; set; }
            public string Company { get; set; }
            public string Password { get; set; }
        }

        //Checking for nulls in the request 
        public class Nulls
        {
            //Checking if RelationCompanynr is null
            public static bool RelationCompanynr(int relationCompanynr)
            {
                if (relationCompanynr == 0)
                    return true;
                else
                    return false;
            }

            //Checking if Relation is null
            public static bool Relation(int relation)
            {
                if (relation == 0)
                    return true;
                else
                    return false;
            }

            //Checking if Company is null
            public static bool Company(string company)
            {
                if (string.IsNullOrEmpty(company))
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
            //Checking RelationCompanynr for SQL injections
            public static bool RelationCompanynr(int relationCompanynr)
            {
                var lower = relationCompanynr.ToString().ToLower();
                for (int x = 0; x < _invalid.Length; x++)
                    if (lower.Contains(_invalid[x]))
                        return true;
                return false;
            }

            //Checking Relation for SQL injections
            public static bool Relation(int relation)
            {
                var lower = relation.ToString().ToLower();
                for (int x = 0; x < _invalid.Length; x++)
                    if (lower.Contains(_invalid[x]))
                        return true;
                return false;
            }

            //Checking Company for SQL injections
            public static bool Company(string company)
            {
                var lower = company.ToString().ToLower();
                for (int x = 0; x < _invalid.Length; x++)
                    if (lower.Contains(_invalid[x]))
                        return true;
                return false;
            }

            //Checking Password for SQL injections
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
            public static bool Info(int relationCompanynr, int relation, string company)
            {
                var t = 0;

                string query = String.Format("SELECT * FROM dbo.creditors WHERE RelationCompanynr = " +
                relationCompanynr + " AND Relation = " + relation + " AND Name = '" + company + "'");

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

                    //If there is 1 result from the query the user is authorized
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

        //Checking if the user already exists in the database
        public static bool Exists(int relation, string company)
        {
            var t = 0;

            string query = String.Format("SELECT * FROM dbo.customers WHERE Customer = " +
            relation + " AND CustomerName = '" + company + "'");

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

                if (t >= 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        
        //When credentials are authenticated a new user is created
        public class Create
        {
            public static void User(int relation, string company, string password)
            {
                string query = String.Format("INSERT INTO dbo.customers (Customer, CustomerName, Password) VALUES (" + relation + ", '" + company + "', '" + password + "')");
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

        //Templates for responses
        public class Response
        {
            //Ok response template
            public static HttpResponseMessage Ok(string description)
            {
                ResponseCode response = new ResponseCode();
                response.code = 200;
                response.info = "OK";
                response.description = description;
                string json = JsonConvert.SerializeObject(response);

                return new HttpResponseMessage(HttpStatusCode.BadRequest)
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

        [FunctionName("createuser")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

	    try
	    {
		//Retrieve request body
		string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                Request request = JsonConvert.DeserializeObject<Request>(requestBody);

                //Saving request body
                int relationCompanynr = request.RelationCompanynr;
                int relation = request.Relation;
                string company = request.Company;
                string password = request.Password;

                //Checking request variables for nulls
                if (Nulls.RelationCompanynr(relationCompanynr))
                    return Response.BadRequest("'RelationCompanynr' is not specified");
                else if (Nulls.Relation(relation))
                    return Response.BadRequest("'Relation' is not specified");
                else if (Nulls.Company(company))
                    return Response.BadRequest("'Company' is not specified");
                else if (Nulls.Password(password))
                    return Response.BadRequest("'Password' is not specified");

                //Checking request variables for SQL injections
                if (SqlInjection.RelationCompanynr(relationCompanynr))
                    return Response.BadRequest("'RelationCompanynr' contains forbidden script");
                else if (SqlInjection.Relation(relation))
                    return Response.BadRequest("'Relation' contains forbidden script");
                else if (SqlInjection.Company(company))
                    return Response.BadRequest("'Company' contains forbidden script");
                else if (SqlInjection.Password(password))
                    return Response.BadRequest("'Password' contains forbidden script");

                //Authenticating user credentials
                if (Authenticate.Info(relationCompanynr, relation, company))
                {
                    //Checking if user already exists
                    if (!Exists(relation, company))
                    {
                        //Creating new user
                        Create.User(relation, company, password);
                        return Response.Ok("User created succesfully");
                    }
                    else
                    {
                        return Response.BadRequest("User already exists");
                    }
                }
                else 
                {
                    return Response.Unauthorized("Credentials are not valid");
                }
            }
            //Fail safe for when the user forgets comma, bracket, etc.
            catch
            {
                return Response.BadRequest("'RelationCompanynr', 'Relation', 'Company' or 'Password' has not been specified properly");
            }
        }
    }
}

