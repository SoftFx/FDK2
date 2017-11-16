using System;
using System.Collections.Generic;
using System.Text;

namespace TickTrader.FDK.Calculator
{
    public class BriefToStringBuilder
    {
        private StringBuilder builder = new StringBuilder();
        private bool isEmpty = true;

        public BriefToStringBuilder()
        {
        }

        public BriefToStringBuilder(object obj)
        {
            if (obj != null)
            {
                if (obj is string)
                    builder.Append(obj);
                else
                    builder.Append(obj.GetType().Name).Append(" =>");
            }
        }

        public void Append(string name, object value, string customFormat = null)
        {
            AppendName(name);
            if (customFormat == null)
                builder.Append(value);
            else
                builder.AppendFormat(customFormat, value);
        }

        public void Append(string name, IEnumerable<string> value)
        {
            AppendName(name);

            builder.Append("{");
            if (value != null)
            {
                builder.Append(string.Join(", ", value));
            }
            builder.Append("}");
        }

        public void AppendNotNull(string name, IEnumerable<string> value)
        {
            if (value != null)
                Append(name, value);
        }

        public void Append<T>(string name, T value, Func<T, string> customFormatAction)
            where T : struct
        {
            DoCustomActionAndAppend<T>(name, value, customFormatAction);
        }

        private void AppendName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("name");

            if (isEmpty)
            {
                if (builder.Length > 0)
                    builder.Append(" ");
                isEmpty = false;
            }
            else
                builder.Append(", ");
            builder.Append(name);
            builder.Append("=");
        }

        public void AppendNotNull(string name, object value, string customFormat = null)
        {
            if (value != null)
                Append(name, value, customFormat);
        }

        public void AppendNotNull<T>(string name, Nullable<T> value, Func<T, string> customFormatAction = null)
            where T: struct
        {
            if (value.HasValue)
            {
                if (customFormatAction != null)
                    DoCustomActionAndAppend<T>(name, value.Value, customFormatAction);
                else
                    Append(name, (object)value.Value);
            }
        }

        private void DoCustomActionAndAppend<T>(string name, T value, Func<T, string> customFormatAction = null)
        {
            string formatedString = customFormatAction(value);
            Append(name, formatedString);
        }

        public string GetResult()
        {
            return builder.ToString();
        }

        public override string ToString()
        {
            return builder.ToString();
        }
    }
}
