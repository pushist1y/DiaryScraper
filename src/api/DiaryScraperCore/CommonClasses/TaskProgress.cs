using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DiaryScraperCore
{
    public abstract class TaskProgress
    {
        public abstract int Percent { get; }
        public bool RangeDiscovered { get; set; }
        [JsonIgnore]
        public string Error { get; set; }
        public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();
        public T GetValue<T>(string name)
        {
            if (!Values.TryGetValue(name, out var val))
            {
                return default(T);
            }
            return (T)Convert.ChangeType(val, typeof(T));
        }

        public void IncrementInt(string name, long addValue)
        {
            if (Values.TryGetValue(name, out var val))
            {
                var intVal = Convert.ToInt32(val);
                Values[name] = intVal + addValue;
            }
            else
            {
                Values[name] = addValue;
            }
        }

        public void SetValue<T>(string name, T val)
        {
            Values[name] = val;
        }

    }
}