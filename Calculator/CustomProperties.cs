using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace TickTrader.FDK.Calculator
{
    [DataContract]
    public class CustomPropertyKey : ICloneable
    {
        [DataMember]
        public string Namespace { get; set; }
        [DataMember]
        public string Name { get; set; }

        #region Constructor

        public CustomPropertyKey()
        {
        }

        public CustomPropertyKey(string ns, string name)
        {
            #region args check
            if (string.IsNullOrEmpty(ns))
                throw new ArgumentNullException("ns");
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            #endregion

            Namespace = ns.Trim().ToLower();
            Name = name.Trim().ToLower();
        }

        #endregion

        public CustomPropertyKey Clone()
        {
            return new CustomPropertyKey(Namespace, Name);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        #region Overrides of equality and get hash code methods

        public override bool Equals(Object obj)
        {
            if (ReferenceEquals(obj, null) || GetType() != obj.GetType())
                return false;

            var otherKey = (CustomPropertyKey)obj;
            return Equals(otherKey);
        }

        public bool Equals(CustomPropertyKey otherKey)
        {
            if ((object)otherKey == null)
                return false;

            return (Namespace == otherKey.Namespace) && (Name == otherKey.Name);
        }

        public override int GetHashCode()
        {
            return Namespace.GetHashCode() ^ Name.GetHashCode();
        }

        public static bool operator == (CustomPropertyKey key1, CustomPropertyKey key2)
        {
            if (ReferenceEquals(key1, key2))
                return true;

            if (((object)key1 == null) || ((object)key2 == null))
                return false;

            return key1.Equals(key2);
        }

        public static bool operator !=(CustomPropertyKey a, CustomPropertyKey b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return string.Format("{0}::{1}", Namespace, Name);
        }
        #endregion
    }

    [DataContract]
    public class CustomProperty
    {
        [DataMember]
        public CustomPropertyKey Key { get; set; }
        [DataMember]
        public String Value { get; set; }

        #region Constructor

        public CustomProperty()
        {
        }

        #endregion

        public CustomProperty Clone()
        {
            return new CustomProperty
            {
                Key = Key.Clone(),
                Value = Value
            };
        }

        public override string ToString()
        {
            return string.Format("{0}=[{1}]", Key, Value ?? string.Empty);
        }
    }

    [DataContract]
    public class CustomProperties : ICloneable
    {
        [DataMember]
        private List<CustomProperty> InnerStorage { get; set; }

        #region Constructor

        public CustomProperties()
        {
        }

        public CustomProperties(List<CustomProperty> properties)
        {
            #region args validation
            if (properties == null)
                throw new ArgumentNullException("properties");
            #endregion

            InnerStorage = properties;
        }

        protected CustomProperties(CustomProperties sourceProperties)
        {
            #region args validation
            if (sourceProperties == null)
                throw new ArgumentNullException("sourceProperties");
            #endregion

            if (sourceProperties.InnerStorage != null)
            {
                InnerStorage = sourceProperties
                    .InnerStorage
                    .Select(p => p.Clone())
                    .ToList();
            }
        }

        #endregion

        public IEnumerable<CustomPropertyKey> Keys
        {
            get
            {
                return GetProperties().Select(p => p.Key).ToList();
            }
        }

        public string Get(CustomPropertyKey key)
        {
            var prop = GetProperties()
                .FirstOrDefault(p => p.Key == key);

            string result = (prop != null)
                ? prop.Value
                : string.Empty;

            return result;
        }

        public CustomProperties Clone()
        {
            return new CustomProperties(this);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public List<CustomProperty> AsList()
        {
            return GetProperties().ToList();
        }

        protected List<CustomProperty> GetProperties()
        {
            return InnerStorage ?? new List<CustomProperty>();
        }


        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (InnerStorage == null)
                InnerStorage = new List<CustomProperty>();
        }

    }

    [DataContract]
    public class CustomPropertiesUpdate : ICloneable
    {
        [DataMember]
        private Dictionary<CustomPropertyKey, string> PropertiesToAddModify = new Dictionary<CustomPropertyKey, string>();
        [DataMember]
        private HashSet<CustomPropertyKey> PropertiesToRemove = new HashSet<CustomPropertyKey>();

        public CustomPropertiesUpdate()
        {
        }

        private CustomPropertiesUpdate(CustomPropertiesUpdate update)
        {
            PropertiesToAddModify = update.PropertiesToAddModify.ToDictionary(k => k.Key.Clone(), v => v.Value);
            PropertiesToRemove = new HashSet<CustomPropertyKey>(update.PropertiesToRemove.Select(p => p.Clone()));
        }

        public bool IsEmpty
        {
            get { return PropertiesToAddModify.Count == 0 && PropertiesToRemove.Count == 0; }
        }

        public void AddModifyProperty(CustomPropertyKey key, string value)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            PropertiesToAddModify[key] = value;
        }

        public void RemoveProperty(CustomPropertyKey key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            PropertiesToRemove.Add(key);
        }

        public List<CustomProperty> GetPropertiesToAddModify()
        {
            var result = (from k in PropertiesToAddModify.Keys
                          let val = PropertiesToAddModify[k]
                          select new CustomProperty { Key = k, Value = val })
                        .ToList();

            return result;
        }

        public List<CustomPropertyKey> GetPropertiesToRemove()
        {
            var result = PropertiesToRemove.ToList();
            return result;
        }

        public CustomProperties ApplyTo(CustomProperties original)
        {
            var processedProperties = new List<CustomProperty>();

            // add new or modified props
            processedProperties.AddRange(GetPropertiesToAddModify());

            // add remaining props which isn't removed or modified
            var remainingProps = from p in original.AsList()
                                 where !PropertiesToAddModify.ContainsKey(p.Key) && !PropertiesToRemove.Contains(p.Key)
                                 select p.Clone();
            processedProperties.AddRange(remainingProps);

            return new CustomProperties(processedProperties);
        }

        public object Clone()
        {
            return new CustomPropertiesUpdate(this);
        }
    }
}
