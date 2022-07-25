using Newtonsoft.Json;
using ParserLes.Model;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserLes
{
    public class Parser
    {
        public const string _connectionString = @"Server=localhost\SQLEXPRESS;Database=ParsingLes;Trusted_Connection=True;";

        List<Deal> dealsAll;
        List<Deal> dealsAllData;
        int dataDealsCount;
        static RestResponse response;
        static string requestUrl = "https://www.lesegais.ru/open-area/graphql";        
        DateTime lastParseTime;
        bool isWorked;
        double tenMinuts;
        int pageSize;
        int productsCounts;
        int numberPage = 0;
        public Parser(int pageSize, int minutese)
        {
            dealsAllData = GetAllDeals();
            dataDealsCount = GetTotalDealsCounts();
            this.dealsAll = new List<Deal>();
            this.pageSize = pageSize;
            tenMinuts = minutese * 60;
            try
            {
                numberPage = GetLastNumberPage();
            }
            catch
            {
                numberPage = 0;
                SaveLastPage(numberPage);
            }
        }

        public void ParseStart()
        {
            isWorked = true;
            FirstParse();
            GC.Collect();
            lastParseTime = DateTime.Now;
            while (isWorked)
            {
                if (DateTime.Now.Subtract(lastParseTime).TotalSeconds >= tenMinuts)
                {
                    SecondParse();
                    lastParseTime = DateTime.Now;
                }
            }
        }

        /// <summary>
        /// Первоначальный парсинг
        /// </summary>
        private void FirstParse()
        {
            ResquestLoadGetDealsCount();
            int dbProductsCount = GetTotalDealsCounts();
            if (productsCounts <= dbProductsCount) return;
            while (productsCounts > 0)
            {
                productsCounts -= pageSize;
                ParseDeals(RequestLoadDeals(numberPage));
                numberPage++;
                SaveLastPage(numberPage);
            }
            SaveAllDeals();
        }

        /// <summary>
        /// Получаем колличество сделок на сайте
        /// </summary>
        void ResquestLoadGetDealsCount()
        {
            var client = new RestClient(requestUrl);
            var request = new RestRequest();
            request.Method = Method.Post;
            request.AddHeader("User-Agent", "C# App");
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("content-type", "application/json");
            request.AddParameter("application/json", "{\r\n  \"query\": \"query SearchReportWoodDealCount($size: Int!, $number: Int!, $filter: Filter, $orders: [Order!]) {\\n  searchReportWoodDeal(filter: $filter, pageable: {number: $number, size: $size}, orders: $orders) {\\n    total\\n    number\\n    size\\n    overallBuyerVolume\\n    overallSellerVolume\\n    __typename\\n  }\\n}\\n\",\r\n  \"variables\": {\r\n    \"size\": 0,\r\n    \"number\": 0,\r\n    \"filter\": null\r\n  },\r\n  \"operationName\": \"SearchReportWoodDealCount\"\r\n}", ParameterType.RequestBody);
            RestResponse response = client.Execute(request);
            var responceContent = response.Content;
            responceContent = responceContent.Substring(responceContent.LastIndexOf("total"));
            responceContent = responceContent.Substring(0, responceContent.LastIndexOf("number"));
            GetTotal(responceContent);
        }

        /// <summary>
        /// Проверяем на вхождение 
        /// </summary>
        /// <param name="deals"></param>
        void ParseDeals(List<Deal> deals)
        {

            if (dataDealsCount != 0)
                for (int i = 0; i < deals.Count; i++)
                {
                    if (IsContain(deals[i].DealNumber))
                    {
                        deals.Remove(deals[i]);
                    }

                }
            if (deals.Count != 0) dealsAll.AddRange(deals);
        }

        /// <summary>
        /// Получаем список 
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private List<Deal> RequestLoadDeals(int number)
        {
            var client = new RestClient(requestUrl);
            var request = new RestRequest();
            request.Method = Method.Post;
            request.AddHeader("User-Agent", "C# App");
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("content-type", "application/json");
            request.AddParameter("application/json", "{\r\n  \"query\": " +
                                "\"query SearchReportWoodDeal($size: Int!, $number: Int!, $filter: Filter, $orders: [Order!])" +
                                " {\\n  searchReportWoodDeal(filter: $filter, pageable: {number: $number, size: $size}, orders: $orders) " +
                                "{\\n   content " +
                                "{\\n   sellerName\\n sellerInn\\n buyerName\\n buyerInn\\n woodVolumeBuyer\\n woodVolumeSeller\\n dealDate\\n dealNumber\\n __typename\\n" +
                                "}\\n   __typename\\n  }\\n}\\n\",\r\n  \"variables\": {\r\n\"size\": " + pageSize.ToString() + ",\r\n \"number\": " + number.ToString() + ",\r\n\"filter\": null,\r\n\"orders\": null\r\n  },\r\n\"operationName\": \"SearchReportWoodDeal\"\r\n}",
                                ParameterType.RequestBody);
            response = client.Execute(request);

            var responce = response.Content;
            responce = responce.Remove(0, responce.IndexOf('['));
            responce = responce.Remove(responce.IndexOf(']') + 1);
            return JsonConvert.DeserializeObject<List<Deal>>(responce);
        }

        /// <summary>
        /// Получаем из строки количество Сделок
        /// </summary>
        /// <param name="total"></param>
        void GetTotal(string total)
        {
            StringBuilder sb = new StringBuilder(total);
            string count = "";
            for (int i = 0; i < sb.Length; i++)
            {
                if (char.IsDigit(sb[i]))
                {
                    count += sb[i].ToString();
                }
            }
            productsCounts = Convert.ToInt32(count);
            int dataBAseCount = 0;
            try
            {
                dataBAseCount = GetTotalDealsCounts();

            }
            catch { dataBAseCount = 0; }

            if (productsCounts > dataBAseCount)
            {
                //baseContext.parserSettings.First().ProductsCount = productsCounts;
                //baseContext.SaveChanges();
            }
        }

        /// <summary>
        /// Последующий парсинг
        /// </summary>
        void SecondParse()
        {
            dealsAll = new List<Deal>();
            ResquestLoadGetDealsCount();
            int dbProductsCount = GetTotalDealsCounts();
            if (productsCounts <= dbProductsCount) return;

            int secProductsCounts = productsCounts - dbProductsCount + pageSize;
            pageSize = productsCounts - dbProductsCount;
            numberPage = productsCounts / pageSize - 1;
            numberPage -= 1;

            while (secProductsCounts > 0)
            {
                productsCounts -= pageSize;
                var newProducts = RequestLoadDeals(numberPage);
                for (int i = 0; i < newProducts.Count; i++)
                {
                    if (!IsContain(newProducts[i].DealNumber))
                    {
                        AddToDB(newProducts[i]);
                    }
                }
                if (newProducts.Count != 0) dealsAll.AddRange(newProducts);

                numberPage++;
                SaveLastPage(numberPage);
            }

        }

        #region SQL

        /// <summary>
        /// Добавление в БД новой сделки
        /// </summary>
        /// <param name="deal"></param>
        public void AddToDB(Deal deal)
        {
            string sqlExpression = $"INSERT INTO [dbo].[WoodDealsTable] ([DealNumber] ,[SellerName] ,[SellerInn] ,[BuyerName] ,[BuyerInn] ,[DealDate] ,[WoodVolumeSeller] ,[WoodVolumeBuyer])" +
                            $"VALUES({deal.DealToString()})";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                command.ExecuteNonQuery();
            }
            Console.SetCursorPosition(0, 1);
            Console.WriteLine("Всего записей: " + GetTotalDealsCounts());
        }

        /// <summary>
        /// Сохраненеие колекции всех сделок
        /// </summary>
        public void SaveAllDeals()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                SqlCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                Deal deal1;
                try
                {
                    foreach (var deal in dealsAll)
                    {
                        deal1 = deal;
                        command.CommandText = $"INSERT INTO [dbo].[WoodDealsTable] ([DealNumber] ,[SellerName] ,[SellerInn] ,[BuyerName] ,[BuyerInn] ,[DealDate] ,[WoodVolumeSeller] ,[WoodVolumeBuyer])" +
                            $"VALUES({deal.DealToString()})";
                        command.ExecuteNonQuery();
                    }
                    transaction.Commit();
                    Console.SetCursorPosition(0, 1);
                    Console.WriteLine("Всего записей: " + GetTotalDealsCounts());
                    dataDealsCount = dealsAll.Count;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    transaction.Rollback();
                }
            }
        }

        /// <summary>
        /// Получить количество всех сделаок в БД
        /// </summary>
        /// <returns></returns>
        public int GetTotalDealsCounts()
        {
            string sqlExpression = $@"SELECT count(*) from WoodDealsTable";
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                int totalCount = (int)command.ExecuteScalar();
                Console.SetCursorPosition(0, 1);
                Console.WriteLine("Всего записей: " + totalCount);
                return totalCount;
            }
        }

        /// <summary>
        /// Получить все сделки из БД
        /// </summary>
        /// <returns></returns>
        private List<Deal> GetAllDeals()
        {
            List<Deal> deals = new List<Deal>();
            string sqlExpression = $@"SELECT * from WoodDealsTable";
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                SqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        string[] values = new string[8];

                        values[0] = reader.GetValue(0).ToString();
                        values[1] = reader.GetValue(1).ToString();
                        values[2] = reader.GetValue(2).ToString();
                        values[3] = reader.GetValue(3).ToString();
                        values[4] = reader.GetValue(4).ToString();
                        values[5] = reader.GetValue(5).ToString();
                        values[6] = reader.GetValue(6).ToString();
                        values[7] = reader.GetValue(7).ToString();

                        deals.Add(new Deal(
                            values[0],
                        values[1],
                        values[2],
                        values[3],
                        values[4],
                        values[5],
                        float.Parse(values[6]),
                        float.Parse(values[7])
                        ));
                    }
                }

            }
            return deals;
        }

        /// <summary>
        /// проверяет на вхождение сделки в БД
        /// </summary>
        /// <param name="dealNumber"></param>
        /// <returns></returns>
        bool IsContain(string dealNumber)
        {
            string sqlExpression = $@"select * from WoodDealsTable
                                    where EXISTS (SELECT DealNumber      
                                    FROM WoodDealsTable 
                                    Where  DealNumber = N'{dealNumber}')";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                SqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    if (reader.GetValue(0).ToString() == dealNumber)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Записывает последнюю использываемую страницу
        /// </summary>
        /// <param name="numberPage"></param>
        private void SaveLastPage(int numberPage)
        {
            string sqlExpression = @"select * from ParserSettings
                                    where EXISTS (SELECT ID      
                                    FROM ParserSettings 
                                    Where  ID = 0)";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                SqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    command.CommandText = $@"Update ParserSettings
                                            SET NumberPage = {numberPage}
                                            Where  ID = 0";
                }
                else
                    command.CommandText = $@"Insert into ParserSettings
                                            ([ID],[NumberPage])
                                           Values (0, {numberPage})";
                reader.Close();
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Получаем последнью страницу, которую парсили, из БД
        /// </summary>
        /// <returns></returns>
        private int GetLastNumberPage()
        {
            string sqlExpression = @"select * from ParserSettings
                                    Where  ID = 0)";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                SqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    return (int)reader.GetValue(0);
                }
            }
            return 0;
        }

        #endregion
    }
}
