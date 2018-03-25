using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Kinetic.Vision.Enumerations
{
    public class EnumStringValue : System.Attribute
    {
        private readonly string _Value;
        public EnumStringValue(string Value)
        {
            this._Value = Value;
        }

        public string Value
        {
            get { return this._Value; }
        }

        public static Enum Parse(Type EnumType, string value)
        {
            IList<KeyValuePair<System.Enum, string>> enumValues = EnumStringValue.ToList(EnumType);

            return enumValues.Where(o => o.Value == value).First().Key;
        }

        public static string GetStringValue(System.Enum value)
        {
            string output = null;
            Type type = value.GetType();

            FieldInfo fi = type.GetField(value.ToString());
            EnumStringValue[] attrs = (EnumStringValue[])fi.GetCustomAttributes(typeof(EnumStringValue), false);

            if (attrs.Length > 0)
                output = attrs[0].Value;

            return output;
        }

		public static IList<EnumValue> ToKendoList(Type enumType)
		{
			if (enumType == null)
				throw new ArgumentNullException("type");

			var list = new List<EnumValue>();
			Array enumValues = System.Enum.GetValues(enumType);

			foreach (System.Enum value in enumValues)
				list.Add(new EnumValue() { EnumString = value.ToString(), Value = GetStringValue(value), Key = Convert.ToInt32(value) });

			return list;
		}

        public static IList<KeyValuePair<System.Enum, string>> ToList(Type enumType)
        {
            if (enumType == null)
                throw new ArgumentNullException("type");

            var list = new List<KeyValuePair<System.Enum, string>>();
            Array enumValues = System.Enum.GetValues(enumType);

            foreach (System.Enum value in enumValues)
                list.Add(new KeyValuePair<System.Enum, string>(value, GetStringValue(value)));

            return list;
        }

        public static IList<KeyValuePair<int, string>> ToIntegerList(Type EnumType)
        {
            if (EnumType == null)
                throw new ArgumentNullException("type");

            List<KeyValuePair<int, string>> list = new List<KeyValuePair<int, string>>();
            Array enumValues = System.Enum.GetValues(EnumType);

            foreach (System.Enum value in enumValues)
                list.Add(new KeyValuePair<int, string>(Convert.ToInt32(value), GetStringValue(value)));

            return list;
        }


    }

	public class EnumValue
	{
		public int Key { get; set; }
		public string Value { get; set; }
		public string EnumString { get; set; }
	}
}
