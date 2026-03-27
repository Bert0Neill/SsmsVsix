using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_Buddy.Services
{
    internal class FileService
    {
        public void SaveToJson<T>(T obj, string filePath)
        {
            string json = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public T LoadFromJson<T>(string filePath)
        {
            if (!File.Exists(filePath)) return default;
            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
