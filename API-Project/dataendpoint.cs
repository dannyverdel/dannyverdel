/* ************************************************************************** */
/*  dataendpoint.cs                                                           */
/*  By: Danny Verdel <danny.verdel@gmail.com                                  */
/*  Created: 2021/04/14 22:50                                                 */
/*  Updated: 2021/04/14 23:05                                                 */
/*                                                                            */
/*  Let users reveive data from rides based on                                */
/*  self specified date range and columns                                     */
/*                                                                            */
/*                                                                            */
/* ************************************************************************** */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Serialization;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ApiDobbeTransport
{
    public class dataendpoint
    {
        //Retrieving connection string
        private static string _connectionString = Environment.GetEnvironmentVariable("ConnectionString");

        //Checking if user forgot to fill out anything
        public class NullCheck
        {
            //Checking if Token is null
            public static bool Token(string token)
            {
                if (string.IsNullOrEmpty(token))
                    return true;
                return false;
            }

            //Checkinf if Date is null
            public static bool Date(string date)
            {
                if (string.IsNullOrEmpty(date))
                    return true;
                return false;
            }

            //Checking if Columns is null
            public static bool Columns(string columns)
            {
                if (string.IsNullOrEmpty(columns))
                    return true;
                return false;
            }

            //Checken if Accept is null
            public static bool Accept(string accept)
            {
                if (string.IsNullOrEmpty(accept) || accept == "*/*")
                    return true;
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
            //Checking for Token
            public static bool Token(string token)
            {
                var lower = token.ToLower();
                for (int x = 0; x < _invalid.Length; x++)
                    if (lower.Contains(_invalid[x]))
                        return true;
                return false;
            }

            //Checking for Date
            public static bool Date(string date)
            {
                var lower = date.ToLower();
                for (int x = 0; x < _invalid.Length; x++)
                    if (lower.Contains(_invalid[x]))
                        return true;
                return false;
            }

            //Checking for Columns
            public static bool Columns(string columns)
            {
                var lower = columns.ToLower();
                for (int x = 0; x < _invalid.Length; x++)
                    if (lower.Contains(_invalid[x]))
                        return true;
                return false;
            }

            //Checking for Accept
            public static bool Accept(string accept)
            {
                var lower = accept.ToLower();
                for (int x = 0; x < _invalid.Length; x++)
                    if (lower.Contains(_invalid[x]))
                        return true;
                return false;
            }
        }

        /*
        Validating token
        User input is being compared to a database
        */
        public class Validate
        {
            public static bool Token(string token)
            {
                var t = 0;

                string query = String.Format("SELECT CreatedToken FROM dbo.customers WHERE Token = '" + token + "'");

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    SqlCommand command = new SqlCommand(query, conn);
                    conn.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                        t++;

                    reader.Close();

                    var created = Convert.ToDateTime(Convert.ToDateTime(command.ExecuteScalar()).ToString("yyyy-MM-dd HH:mm:ss.fff"));

                    command.Dispose();
                    conn.Close();

                    if (t == 1)
                    {
                        var now = Convert.ToDateTime(DateTime.Now).ToString("yyyy-MM-dd HH:mm:ss.fff");

                        double hours = DateTime.Now.Subtract(created).TotalHours;

                        if (hours < 1)
                            return true;
                        else
                            return false;
                    }
                    else
                        return false;
                }
            }
        }

        //Checking if user input matches required format
        public class FormatCheck
        {
            //Checking for Date
            public static bool Date(string date)
            {
                if (date == "maand" || date == "week" || date == "dag")
                    return true;
                else
                {
                    try
                    {
                        if (date.Contains('/'))
                        {
                            string[] findate = date.Split('/');
                            if (string.IsNullOrEmpty(findate[0]) || string.IsNullOrEmpty(findate[1]) || findate.Length > 2 || findate.Length < 2)
                                return false;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            //Checking for Columns
            public static bool Columns(string columns)
            {
                if (columns.Contains(","))
                {
                    if (columns[columns.Length - 1] == ',' || columns[columns.Length - 1] == ' ')
                        return false;
                    else
                    {
                        string[] columnArr = columns.Split(",");
                        if (columnArr.Length == 1)
                            return false;
                        else
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    if (columns.Contains(" "))
                    {
                        return false;
                    }
                    return true;
                }
            }

            //Checking for Accept
            public static bool Accept(string accept)
            {
                if (accept == "application/json" || accept == "application/excel" || accept == "application/xml")
                    return true;
                else
                    return false;
            }
        }

        //Update counter to know how many times a user used the API
        public class Counter
        {
            //Get current count
            public static int Get(string token)
            {
                string query = string.Format("SELECT ApiCalls FROM dbo.customers WHERE token = '" + token + "'");

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    SqlCommand command = new SqlCommand(query, conn);
                    conn.Open();
                    int count = (int)command.ExecuteScalar();
                    command.Dispose();
                    conn.Close();

                    return count;
                }
            }

            //Incrementing and updating counter
            public static void Update(int count, string token)
            {
                count = count + 1;
                string query = string.Format("UPDATE dbo.customers SET ApiCalls = " + count + " WHERE token = '" + token + "'");

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    SqlCommand command = new SqlCommand(query, conn);
                    conn.Open();
                    command.ExecuteNonQuery();
                    command.Dispose();
                    conn.Close();
                }
            }
        }

        //Retrieving Customer
        public class Get
        {
            //Ophalen van klant nummer
            public static int Customer(string token)
            {
                string query = String.Format("SELECT Customer FROM dbo.customers WHERE Token = '" + token + "'");

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    SqlCommand command = new SqlCommand(query, conn);
                    conn.Open();

                    int Customer = (int)command.ExecuteScalar();

                    command.Dispose();
                    conn.Close();

                    return Customer;
                }
            }

            //Retrieving Date ranges
            public static string[] Date(string[] date)
            {
                if (date[0] == "dag")
                {
                    string[] findate = { DateTime.Now.ToString("yyyy-MM-dd") };
                    return findate;
                }
                else if (date[0] == "week")
                {
                    List<string> dates = new List<string>();

                    var today = DateTime.Now.Date;
                    var day = (int)today.DayOfWeek;
                    const int totalDaysOfWeek = 7;
                    for (var i = -day; i < -day + totalDaysOfWeek; i++)
                        dates.Add((today.AddDays(i).Date).ToString("yyyy-MM-dd"));

                    string[] findate = new string[2];
                    findate[0] = dates[0];
                    findate[1] = dates[6];

                    return findate;
                }
                else if (date[0] == "maand")
                {
                    DateTime now = DateTime.Now;
                    var startDate = new DateTime(now.Year, now.Month, 1);
                    var endDate = startDate.AddMonths(1).AddDays(-1);

                    string[] findate = new string[2];
                    findate[0] = startDate.ToString("yyyy-MM-dd");
                    findate[1] = endDate.ToString("yyyy-MM-dd");

                    return findate;
                }
                else
                {
                    return date;
                }
            }
        }

        //Converting input
        public class Converten
        {
            //Converteren van datum
            public static string[] Date(string date)
            {
                if (date == "maand" || date == "week" || date == "dag")
                {
                    string[] findate = { date };
                    return findate;
                }
                else
                {
                    string[] findate = date.Split('/');
                    for (int x = 0; x < 2; x++)
                        Convert.ToDateTime(findate[x].Trim()).ToString("yyyy-MM-dd").Trim();
                    return findate;
                }
            }

            //Converteren van kolommen
            public static string[] Columns(string columns)
            {
                if (columns.Contains(","))
                {
                    string[] columnArr = columns.Split(",");
                    return columnArr;
                }
                else
                {
                    string[] columnArr = { columns };
                    return columnArr;
                }
            }
        }

        //Creating and filling DataTable
        public class Table
        {
            //Creating the DataTable
            public static DataTable Create(int customer, string[] columns)
            {
                DataTable dt = new DataTable("Customer " + customer);

                string[] valid = { "CustomerName",  "LoadingDate", "LoadingPlannedTime", "UnloadingDate", "UnloadingName", "UnloadingAddress", "UnloadingZipcode",
                "UnloadingCity", "UnloadingCountry", "UnloadingReference", "UnloadingPlannedTime", "UnloadingStartTime", "UnloadingEndTime", "Loadingmeter",
                "DeliveryStatus", "Amount", };

                dt.Columns.Add("FinDate");

                int validCount = 0;

                for (int i = 0; i < columns.Length; i++)
                {
                    for (int x = 0; x < valid.Length; x++)
                    {
                        if (columns[i].Trim() == valid[x])
                        {
                            dt.Columns.Add(columns[i].Trim());
                            validCount++;
                        }
                    }
                }

                if (validCount != columns.Length)
                    return null;
                else
                    return dt;
            }

            //Filling the DataTable
            public static DataTable Fill(string columns, DataTable dt, string query)
            {
                using var adp = new SqlDataAdapter(query, _connectionString);
                using DataTable datatable = new DataTable();
                adp.Fill(datatable);
                var temp = datatable;

                string[] Columns = columns.Split(',');

                foreach (DataRow item in datatable.Rows)
                {
                    DataRow dr = dt.NewRow();
                    for (int i = 1; i <= Columns.Length; i++)
                    {
                        dr[0] = item["FinDate"];
                        dr[i] = item[i].ToString();
                    }
                    dt.Rows.Add(dr);
                }

                return dt;
            }
        }

        //Basic template for response codes
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

        //Creating responses
        public class Response
        {
            //Basic template for Bad Request
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

            //Basic template for unauthorized
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

            //Converting DataTable to JSON and returning it
            public static HttpResponseMessage Json(DataSet ds)
            {
                string json = JsonConvert.SerializeObject(ds, Formatting.Indented);

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            }

            //Converting DataTable to excel file and returning it
            public static HttpResponseMessage Excel(int customer, DataSet ds)
            {
                var result = new HttpResponseMessage(HttpStatusCode.OK);
                var workbookBytes = new byte[0];
                using (XLWorkbook wb = new XLWorkbook())
                {
                    for (int i = 0; i < ds.Tables.Count; i++)
                    {
                        wb.Worksheets.Add(ds.Tables[i], ds.Tables[i].TableName);
                    }
                    wb.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    wb.Style.Font.Bold = true;

                    foreach (var ws in wb.Worksheets)
                    {
                        ws.ColumnWidth = 25;
                        ws.RowHeight = 25;
                        ws.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }

                    using (var ms = new MemoryStream())
                    {
                        wb.SaveAs(ms);
                        workbookBytes = ms.ToArray();
                    }

                    result.Content = new ByteArrayContent(workbookBytes);
                    result.Content.Headers.ContentDisposition =
                        new ContentDispositionHeaderValue("attachment") { FileName = customer + ".xlsx" };
                    result.Content.Headers.ContentType =
                        new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                    return result;
                }
            }

            //Converting DataTable to XML
            public static HttpResponseMessage XML(DataSet ds)
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (TextWriter streamWriter = new StreamWriter(memoryStream))
                    {
                        var xmlSerializer = new XmlSerializer(typeof(DataSet));
                        xmlSerializer.Serialize(streamWriter, ds);
                        var xmlString = Encoding.UTF8.GetString(memoryStream.ToArray());

                        return new HttpResponseMessage(HttpStatusCode.OK) {
                               Content = new StringContent(xmlString, Encoding.UTF8, "application/xml")
                        };
                    }
                }
            }
        }

        [FunctionName("dataendpoint")]
        public static HttpResponseMessage Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            //Retrieving request parameters
            string token = req.Headers["Authentication"];
            string date = req.Query["date"];
            string columns = req.Query["columns"];
            string accept = req.Headers["Accept"];

            //Checking for nulls
            if (NullCheck.Token(token))
                return Response.BadRequest("'Authentication' is niet ingevuld");
            else if (NullCheck.Date(date))
                return Response.BadRequest("'date' is niet ingevuld");
            else if (NullCheck.Accept(accept))
                return Response.BadRequest("'Accept' type is niet ingevuld");
            else if (NullCheck.Columns(columns))
                return Response.BadRequest("'columns' is niet ingevuld");

            //Checking for sql keywords
            if (SqlInjection.Token(token))
                return Response.BadRequest("'Authentication' bevat niet toegestane script");
            else if (SqlInjection.Date(date))
                return Response.BadRequest("'date' bevat niet toegestane script");
            else if (SqlInjection.Accept(accept))
                return Response.BadRequest("'Accept' bevat niet toegestane script");
            else if (SqlInjection.Columns(columns))
                return Response.BadRequest("'columns' bevat niet toegestane script");

            //Checking if Token matches
            if (!Validate.Token(token))
                return Response.Unauthorized("Token komt niet overeen");

            //Retrieving and updating counter
            int count = Counter.Get(token);
            Counter.Update(count, token);

            //Checking if format is correct
            if (!FormatCheck.Date(date))
                return Response.BadRequest("Ingevoerde 'date' klopt niet");
            else if (!FormatCheck.Accept(accept))
                return Response.BadRequest("Ingevoerde 'Accept' type klopt niet, dit moet 'application/json' of 'application/excel' zijn");
            else if (!FormatCheck.Columns(columns))
                return Response.BadRequest("Ingevoerde 'columns' kloppen niet");

            //Retrieving Customer
            int customer = Get.Customer(token);

            //Converting variables
            string[] dateArr = Converten.Date(date);
            string[] columnArr = Converten.Columns(columns);

            //Creating the DataTable
            DataTable dt = Table.Create(customer, columnArr);
            if (dt != null)
            {
                //Retrieving Dates
                string[] findate = Get.Date(dateArr);

                //Choosing query based on chosen date range
                string query = null;
                if (findate.Length == 1)
                    query = String.Format("SELECT FinDate, " + columns + " FROM dbo.tb_SO_Leg WHERE Customer = "
                            + customer + " AND FinDate = '" + findate[0] + "' ORDER BY FinDate ASC");
                else
                    query = String.Format("SELECT FinDate, " + columns + " FROM dbo.tb_SO_Leg WHERE Customer = "
                            + customer + " AND FinDate BETWEEN '" + findate[0] + "' AND '" + findate[1] + "' ORDER BY FinDate ASC");

                //Filling DataTable
                dt = Table.Fill(columns, dt, query);
                DataSet ds = new DataSet();
                ds.Tables.Add(dt);

                //Returning DataTable in specified accept type
                if (accept == "application/json")
                    return Response.Json(ds);
                else if (accept == "application/excel")
                    return Response.Excel(customer, ds);
                else
                    return Response.XML(ds);
            }
            //Fail safe for when the user forgets to specify a comma, bracket, etc.
            else
            {
                return Response.BadRequest("U heeft ongeldige 'columns' gespecificeerd");
            }
        }
    }
}
