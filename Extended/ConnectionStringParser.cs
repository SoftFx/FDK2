namespace TickTrader.FDK.Extended
{
    using System;
    using System.Text;
    using System.Collections;
    using System.Collections.Specialized;

    class ConnectionStringParser
    {
        /// <summary>
        /// Sets all string properties to empty value.
        /// </summary>
        public ConnectionStringParser()
        {
            orderedDictionary_ = new OrderedDictionary();
        }

        public void Parse(string connectionString)
        {
            orderedDictionary_.Clear();

            int pos = 0;

            while (pos < connectionString.Length)
            {
                if (connectionString[pos] != '[')
                    throw new Exception("Invalid connection string");

                ++ pos;

                if (pos == connectionString.Length)
                    throw new Exception("Invalid connection string");

                int typePos = pos;

                while (true)
                {
                    if (connectionString[pos] == ']')
                        break;

                    ++ pos;

                    if (pos == connectionString.Length)
                        throw new Exception("Invalid connection string");
                }

                string type = connectionString.Substring(typePos, pos - typePos);

                ++ pos;

                if (pos == connectionString.Length)
                    throw new Exception("Invalid connection string");

                int keyPos = pos;

                while (true)
                {
                    if (connectionString[pos] == '=')
                        break;

                    ++ pos;

                    if (pos == connectionString.Length)
                        throw new Exception("Invalid connection string");
                }

                string key = connectionString.Substring(keyPos, pos - keyPos);

                ++ pos;

                int valuePos = pos;

                while (true)
                {
                    if (pos == connectionString.Length)
                        break;

                    if (connectionString[pos] == ';')
                        break;

                    ++ pos;
                }

                string value = connectionString.Substring(valuePos, pos - valuePos);

                if (type == "String")
                {
                    orderedDictionary_[key] = value;
                }
                else if (type == "Int32")
                {
                    orderedDictionary_[key] = int.Parse(value);
                }
                else if (type == "Real")
                {
                    orderedDictionary_[key] = double.Parse(value);
                }
                else if (type == "Boolean")
                {
                    orderedDictionary_[key] = bool.Parse(value);
                }
                else
                    throw new Exception("Invalid connection string");

                if (pos == connectionString.Length)
                    break;

                ++ pos;
            }
        }

        public bool TryGetStringValue(string key, out string value)
        {
            object objectValue = orderedDictionary_[key];

            if (objectValue == null)
            {
                value = null;

                return false;
            }

            value = (string) objectValue;
            
            return true;
        }

        public bool TryGetIntValue(string key, out int value)
        {
            object objectValue = orderedDictionary_[key];

            if (objectValue == null)
            {
                value = 0;

                return false;
            }

            value = (int) objectValue;
            
            return true;
        }

        public bool TryGetDoubleValue(string key, out double value)
        {
            object objectValue = orderedDictionary_[key];

            if (objectValue == null)
            {
                value = 0;

                return false;
            }

            value = (double) objectValue;
            
            return true;
        }

        public bool TryGetBoolValue(string key, out bool value)
        {
            object objectValue = orderedDictionary_[key];

            if (objectValue == null)
            {
                value = false;

                return false;
            }

            value = (bool) objectValue;
            
            return true;
        }

        OrderedDictionary orderedDictionary_;
    }
}
