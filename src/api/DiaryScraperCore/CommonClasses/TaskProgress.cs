using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DiaryScraperCore
{
    public abstract class TaskProgress
    {
        private readonly string _currentValueName;
        private readonly string _totalValueName;
        public TaskProgress(string currentValueName, string totalValueName)
        {
            _currentValueName = currentValueName;
            _totalValueName = totalValueName;
            Values[currentValueName] = 0;
            Values[totalValueName] = 0;
        }

        public virtual void SetTotal(int total)
        {
            Values[_totalValueName] = total;
            RangeDiscovered = true;
        }

        public virtual void Step(int step = 1)
        {
            IncrementInt(_currentValueName, step);
        }

        public virtual int Percent
        {
            get
            {
                var disc = GetValue<int>(_totalValueName);
                var proc = GetValue<int>(_currentValueName);
                if (disc == 0)
                {
                    return RangeDiscovered ? 100 : 0;
                }
                return Convert.ToInt32(100.0 * proc / disc);
            }
        }
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