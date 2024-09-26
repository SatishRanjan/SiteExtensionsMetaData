using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteExtesionInstalltionKuduConsoleApp
{
    public class JsonSettings
    {
        private readonly static TimeSpan _timeout = TimeSpan.FromSeconds(5);
        private readonly string _path;

        public JsonSettings(string path)
        {
            _path = path;
        }

        public string GetValue(string key)
        {
            return Read().Value<string>(key);
        }

        public IEnumerable<KeyValuePair<string, JToken>> GetValues()
        {
            var dict = (IDictionary<string, JToken>)Read();
            return dict.ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        public void SetValue(string key, JToken value)
        {
            JObject json = Read();
            json[key] = value;
            Save(json);
        }

        public void SetValues(JObject values)
        {
            JObject json = Read();
            foreach (KeyValuePair<string, JToken> pair in values)
            {
                json[pair.Key] = pair.Value;
            }

            Save(json);
        }

        public void SetValues(IEnumerable<KeyValuePair<string, JToken>> values)
        {
            JObject json = Read();
            foreach (KeyValuePair<string, JToken> pair in values)
            {
                json[pair.Key] = pair.Value;
            }

            Save(json);
        }

        public bool DeleteValue(string key)
        {
            JObject json = Read();
            if (json.Remove(key))
            {
                Save(json);
                return true;
            }

            return false;
        }

        public void Save(JObject json)
        {
            if (!File.Exists(_path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path));
            }

            // opens file for FileAccess.Write but does allow other dirty read (FileShare.Read).
            // it is the most optimal where write is infrequent and dirty read is acceptable.
            using (var writer = new JsonTextWriter(new StreamWriter(File.Open(_path, FileMode.Create, FileAccess.Write, FileShare.Read))))
            {
                // prefer indented-readable format
                writer.Formatting = Formatting.Indented;
                json.WriteTo(writer);
            }
        }

        public override string ToString()
        {
            // JObject.ToString() : Returns the indented JSON for this token.
            return Read().ToString(Formatting.None);
        }

        private JObject Read()
        {
            // need to check file exist before acquire lock
            // since acquire lock will generate lock file, and if folder not exist, will create folder
            if (!File.Exists(_path))
            {
                return new JObject();
            }
            // opens file for FileAccess.Read but does allow other read/write (FileShare.ReadWrite).
            // it is the most optimal where write is infrequent and dirty read is acceptable.
            using (var reader = new JsonTextReader(new StreamReader(File.Open(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))))
            {
                try
                {
                    return JObject.Load(reader);
                }
                catch (JsonException)
                {
                    // reset if corrupted.
                    return new JObject();
                }
            }
        }
    }
}
