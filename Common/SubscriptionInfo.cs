using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TickTrader.FDK.Common
{
    /// <summary>
    /// Contains subscription parameters.
    /// </summary>
    public class SubscriptionInfo
    {
        public enum QuoteStreamCompressionTypes
        {
            WithoutCompression = 0,
            Snappy = 1,
            Unknown = 100
        }

        #region Properties

        /// <summary>
        /// Gets subscription name.
        /// </summary>
        //// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                this.name = value;
            }
        }

        /// <summary>
        /// Gets subscription frequency filter param in ms.
        /// </summary>
        //// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public double FrequencyFilterMs
        {
            get
            {
                return this.frequencyFilterMs;
            }
            set
            {
                this.frequencyFilterMs = value;
            }
        }

        /// <summary>
        /// Gets subscription total depth limit.
        /// </summary>
        //// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public int TotalDepthLimit
        {
            get
            {
                return this.totalDepthLimit;
            }
            set
            {
                this.totalDepthLimit = value;
            }
        }

        /// <summary>
        /// Gets subscription compression param.
        /// </summary>
        ///// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public QuoteStreamCompressionTypes Compression
        {
            get
            {
                return this.compression;
            }
            set
            {
                this.compression = value;
            }
        }
        #endregion

        /// <summary>
        /// Converts SubscriptionInfo to string; format is 'Name = {0}; FrequencyFilterMs = {1}; TotalDepthLimit = {2}; Compression = {3}'
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("Name = {0}; FrequencyFilterMs = {1}; TotalDepthLimit = {2}; Compression = {3}", this.Name, this.FrequencyFilterMs, this.TotalDepthLimit, this.Compression);
        }

        #region Members

        string name;
        double frequencyFilterMs;
        int totalDepthLimit;
        QuoteStreamCompressionTypes compression;
        #endregion
    }
}
