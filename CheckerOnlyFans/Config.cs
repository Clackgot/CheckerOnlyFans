using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace CheckerOnlyFans
{

    class Config
    {
        string configName;

        public string ApiKey { get; }

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
        public Config() : this("config.json") { }

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
}