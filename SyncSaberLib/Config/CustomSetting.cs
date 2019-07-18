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

        public abstract object GetValue();
    }

    public class CustomSetting<T>
        : CustomSetting
    {
        public T Value { get; set; }
        public override object GetValue()
        {
            return Value;
        }
    }

    public static class CustomSettingExtensions
    {
        public static T Value<T>(this CustomSetting setting)
        {
            return (T)setting.GetValue();
        }
    }


}
