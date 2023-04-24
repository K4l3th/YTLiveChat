using System.Text;
using System.Net;
using System.Text.Json;
using Newtonsoft.Json;
using System.Collections;

class Util
{
    public static string generateClientMessageId() {
        string base_ = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-";
        StringBuilder sb = new StringBuilder();
        Random random = new Random();

        for (int i = 0; i < 26; i++) {
            sb.Append(base_.ElementAt(random.Next(base_.Length)));
        }

        return sb.ToString();
    }

    private static void putRequestHeader(Dictionary<string, string> header) {
        header.Add("Accept-Charset", "utf-8");
        header.Add("User-Agent", YouTubeLiveChat.userAgent);
    }

    public static string getPageContent(string url, Dictionary<string, string> headers) {
        try {
            var request = WebRequest.Create(url);

            request.Method = "GET";
            putRequestHeader(headers);
            headers.ToList().ForEach(x => request.Headers.Add(x.Key, x.Value));

            string text;
            var response = (HttpWebResponse)request.GetResponse();

            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                text = sr.ReadToEnd();
            }

            return text;
        } catch(Exception e) {
            Console.WriteLine(e.Message);
            return null;
        }
    }

    public static string getPageContentWithJson(string url, string postData, Dictionary<string, string> headers) {
        try {
            var request = WebRequest.Create(url);

            request.ContentType = "application/json";

            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] data = encoding.GetBytes(postData);
            request.ContentLength = data.Length;

            request.Method = "POST";
            putRequestHeader(headers);
            headers.ToList().ForEach(x => request.Headers.Add(x.Key, x.Value));

            Stream newStream = request.GetRequestStream(); //open connection
            newStream.Write(data, 0, data.Length); // Send the data.
            newStream.Close();

            string text;
            var response = (HttpWebResponse)request.GetResponse();

            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                text = sr.ReadToEnd();
            }

            return text;
        } catch(Exception e) {
            Console.WriteLine(e.Message);
            return null;
        }
    }

    public static string toJSON(Dictionary<string, Object> json) {
        return System.Text.Json.JsonSerializer.Serialize(json);
    }

    public static Dictionary<string, object> toJSON(string json) {
        return JsonConvert.DeserializeObject<Dictionary<string, object>>(json, new DictionaryConverter());
    }

    public static Dictionary<string, Object> getJSONMap(Dictionary<string, Object> json, string[] keys) {
        Dictionary<string, Object> map = json;
        foreach (string key in keys) {
            if (map.ContainsKey(key)) {
                map = (Dictionary<string, Object>) map[key];
            } else {
                return null;
            }
        }
        return map;
    }

    public static Dictionary<string, Object> getJSONMap(Dictionary<string, Object> json, Object[] keys) {
        Dictionary<string, Object> map = json;
        List<Object> list = null;
        foreach (Object key in keys) {
            if (map != null) {
                if (map.ContainsKey(key.ToString())) {
                    object value = map[key.ToString()];
                    if (value is IList && value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>))) {
                        list = (List<Object>) value;
                        map = null;
                    } else {
                        map = (Dictionary<string, Object>) value;
                    }
                } else {
                    return null;
                }
            } else {
                map = (Dictionary<string, Object>) list[(int) key];
                list = null;
            }
        }
        return map;
    }

    public static List<Object> getJSONList(Dictionary<string, Object> json, string listKey, string[] keys) {
        Dictionary<string, Object> Dictionary = getJSONMap(json, keys);
        if (Dictionary != null && Dictionary.ContainsKey(listKey)) {
            return (List<Object>) Dictionary[listKey];
        }
        return null;
    }

    public static Object getJSONValue(Dictionary<string, Object> json, string key) {
        if (json != null && json.ContainsKey(key)) {
            return json[key];
        }
        return null;
    }

    public static string getJSONValueString(Dictionary<string, Object> json, string key) {
        Object value = getJSONValue(json, key);
        if (value != null) {
            return value.ToString();
        }
        return null;
    }

    public static bool getJSONValueBoolean(Dictionary<string, Object> json, string key) {
        Object value = getJSONValue(json, key);
        if (value != null) {
            return (bool) value;
        }
        return false;
    }

    public static long getJSONValueLong(Dictionary<string, Object> json, string key) {
        Object value = getJSONValue(json, key);
        if (value != null) {
            return (long) value;
        }
        return 0;
    }

    public static int getJSONValueInt(Dictionary<string, Object> json, string key) {
        return (int) getJSONValueLong(json, key);
    }
}