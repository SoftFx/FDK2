using System;

namespace TickTrader.FDK.Client
{
    public class ProtocolSpec
    {
        public ProtocolVersion CurrentOrderEntryVersion { get; private set; }
        public ProtocolVersion CurrentTradeCaptureVersion { get; private set; }
        public ProtocolVersion CurrentQuoteFeedVersion { get; private set; }
        public ProtocolVersion CurrentQuoteStoreVersion { get; private set; }

        public void InitOrderEntryVersion(ProtocolVersion version)
        {
            CurrentOrderEntryVersion = version;

            SupportsOrderReplaceQtyChange = version.GreaterThanOrEqualTo(10, 8);
            SupportsOffTimeDisabledFeatures = version.GreaterThanOrEqualTo(10, 9);
            SupportsNotificationReportSplit = version.GreaterThanOrEqualTo(10, 11);
        }

        public void InitTradeCaptureVersion(ProtocolVersion version)
        {
            CurrentTradeCaptureVersion = version;
            SupportsStockEventFields = version.GreaterThanOrEqualTo(8, 6);
        }

        public void InitQuoteFeedVersion(ProtocolVersion version)
        {
            CurrentQuoteFeedVersion = version;

            SupportsOffTimeDisabledFeatures = version.GreaterThanOrEqualTo(4, 10);
            SupportsSymbolSlippageType = version.GreaterThanOrEqualTo(4, 13);
            SupportsSymbolExtendedName = version.GreaterThanOrEqualTo(4, 14);
            SupportsCurrencyTypeInfo = version.GreaterThanOrEqualTo(4, 16);
        }

        public void InitQuoteStoreVersion(ProtocolVersion version)
        {
            CurrentQuoteStoreVersion = version;

            SupportsVWAPTickList = version.GreaterThanOrEqualTo(3, 2);
            SupportsStockEventQHModifierList = version.GreaterThanOrEqualTo(3, 3);
            SupportsQuoteStoreTickType = version.GreaterThanOrEqualTo(3, 4);
            SupportsBarHistoryBySymbolsList = version.GreaterThanOrEqualTo(3, 7);
        }

        public bool SupportsVWAPTickList { get; private set; }

        public void CheckSupportedVWAPTickList()
        {
            if (!SupportsVWAPTickList)
                throw new NotSupportedException("SplitList is not supported due to the server protocol version!");
        }

        public bool SupportsStockEventQHModifierList { get; private set; }
        public bool SupportsQuoteStoreTickType { get; private set; }
        public bool SupportsBarHistoryBySymbolsList { get; private set; }

        public void CheckSupportedStockEventQHModifierList()
        {
            if (!SupportsStockEventQHModifierList)
                throw new NotSupportedException("StockEventQHModifierList is not supported due to the server protocol version!");
        }

        public bool SupportsOrderReplaceQtyChange { get; private set; }

        public void CheckSupportedOrderReplaceQtyChange()
        {
            if (!SupportsOrderReplaceQtyChange)
                throw new NotSupportedException("Field QtyChange is not supported due to the server protocol version!");
        }

        public bool SupportsOffTimeDisabledFeatures { get; private set; }
        public bool SupportsSymbolSlippageType { get; private set; }
        public bool SupportsSymbolExtendedName { get; private set; }
        public bool SupportsCurrencyTypeInfo { get; private set; }

        public void CheckSupportedOffTimeDisabledFeatures()
        {
            if (!SupportsOffTimeDisabledFeatures)
                throw new NotSupportedException("OffTimeDisabledFeatures is not supported due to the server protocol version!");
        }

        public bool SupportsStockEventFields { get; private set; }
        public bool SupportsNotificationReportSplit { get; private set; }
    }

    public class ProtocolVersion
    {
        public ProtocolVersion()
        {
        }

        public ProtocolVersion(int major, int minor)
        {
            this.Major = major;
            this.Minor = minor;
        }

        public static implicit operator ProtocolVersion(string versionStr)
        {
            string[] parts = versionStr.Split('.');

            if (parts.Length == 2)
            {
                try
                {
                    int major = int.Parse(parts[0]);
                    int minor = int.Parse(parts[1]);
                    return new ProtocolVersion(major, minor);
                }
                catch (FormatException) { }
                catch (OverflowException) { }
            }

            throw new Exception("Invalid version string.");
        }

        public static bool operator <(ProtocolVersion spc1, ProtocolVersion spc2)
        {
            return spc1.LessThan(spc2.Major, spc2.Minor);
        }

        public static bool operator >(ProtocolVersion spc1, ProtocolVersion spc2)
        {
            return spc1.GreaterThan(spc2.Major, spc2.Minor);
        }

        public static bool operator ==(ProtocolVersion spc1, ProtocolVersion spc2)
        {
            return spc1.EqualTo(spc2.Major, spc2.Minor);
        }

        public static bool operator !=(ProtocolVersion spc1, ProtocolVersion spc2)
        {
            return !spc1.EqualTo(spc2.Major, spc2.Minor);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ProtocolVersion)) return false;
            return this == (ProtocolVersion)obj;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (this.Major.GetHashCode() * 397) ^ this.Minor.GetHashCode();
            }
        }

        public static bool operator <=(ProtocolVersion spc1, ProtocolVersion spc2)
        {
            return spc1.LessThanOrEqualTo(spc2.Major, spc2.Minor);
        }

        public static bool operator >=(ProtocolVersion spc1, ProtocolVersion spc2)
        {
            return spc1.GreaterThanOrEqualTo(spc2.Major, spc2.Minor);
        }

        public static int Compare(ProtocolVersion spc1, ProtocolVersion spc2)
        {
            return Compare(spc1, spc2.Major, spc2.Minor);
        }

        public static int Compare(ProtocolVersion spc, int major, int minor)
        {
            if (spc.Major < major)
                return -1;
            if (spc.Major > major)
                return 1;

            if (spc.Minor < minor)
                return -1;
            if (spc.Minor > minor)
                return 1;

            return 0;
        }

        public bool EqualTo(int major, int minor)
        {
            return Major == major && Minor == minor;
        }

        public bool GreaterThan(int major, int minor)
        {
            return Compare(this, major, minor) > 0;
        }

        public bool GreaterThanOrEqualTo(int major, int minor)
        {
            return Compare(this, major, minor) >= 0;
        }

        public bool LessThan(int major, int minor)
        {
            return Compare(this, major, minor) < 0;
        }

        public bool LessThanOrEqualTo(int major, int minor)
        {
            return Compare(this, major, minor) <= 0;
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}", Major, Minor);
        }

        public int Major { get; private set; }

        public int Minor { get; private set; }
    }
}