using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncSaberLib.Config
{
    public abstract class CustomSetting
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (string.IsNullOrEmpty(value?.Trim()))
                    throw new ArgumentException("CustomSetting Name cannot be null or empty.");
                _name = value;
            }
        }
        public string Description { get; set; }
        public bool Required { get; set; }
        public bool Recommended { get; set; }
        public abstract object GetValue();
        public abstract void SetValue(object value);
    }

    public class CustomSetting<T>
        : CustomSetting
    {
        public T Value { get; set; }
        public override object GetValue()
        {
            return Value;
        }
        public override void SetValue(object value)
        {
            if (!typeof(T).IsAssignableFrom(value.GetType()))
                throw new InvalidCastException($"Cannot convert {value.GetType()} to type {typeof(T).ToString()}.");
            Value = (T)value;
        }
    }

    public static class CustomSettingExtensions
    {
        public static T Value<T>(this CustomSetting setting)
        {
            if (!typeof(T).IsAssignableFrom(setting.GetValue().GetType()))
                throw new InvalidCastException($"Cannot convert {setting.GetValue().GetType()} to type {typeof(T).ToString()}.");
            return (T)setting.GetValue();
        }
        
        public static void AddOrUpdate(this Dictionary<string, CustomSetting> dict, CustomSetting setting)
        {
            if (dict.ContainsKey(setting.Name))
                dict[setting.Name].SetValue(setting.GetValue());
            else
                dict.Add(setting.Name, setting);
        }
    }


}
