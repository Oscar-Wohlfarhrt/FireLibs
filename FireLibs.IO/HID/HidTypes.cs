/*
 * Modified by: Oscar-Wohlfarhrt - Github: https://github.com/Oscar-Wohlfarhrt
 * Original from: jhebb and JoshWobbles - Github: https://github.com/InputMapper/Dualshock4
*/

using static FireLibs.IO.HID.NativeMethods;

namespace FireLibs.IO.HID
{
    public enum ReadStatus
    {
        Success = 0,
        WaitTimedOut = 1,
        WaitFail = 2,
        NoDataRead = 3,
        ReadError = 4,
        NotConnected = 5
    }
    public class DSHidInfo
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public string Id { get; set; }
        public int VendorId { get; set; }
        public int ProductId { get; set; }

        public DSHidInfo(string path, string name) : this(path, name, "", 0, 0) { }
        public DSHidInfo(string path, string name, int vendorId, int productId) : this(path, name, "", vendorId, productId) { }

        public DSHidInfo(string path, string name, string id, int vendorId, int productId)
        {
            Path = path;
            Name = name;
            Id = id;
            VendorId = vendorId;
            ProductId = productId;
        }
    }

    public class HidDeviceAttributes
    {
        internal HidDeviceAttributes(HIDD_ATTRIBUTES attributes)
        {
            VendorId = attributes.VendorID;
            ProductId = attributes.ProductID;
            Version = attributes.VersionNumber;

            VendorHexId = "0x" + attributes.VendorID.ToString("X4");
            ProductHexId = "0x" + attributes.ProductID.ToString("X4");
        }

        public int VendorId { get; private set; }
        public int ProductId { get; private set; }
        public int Version { get; private set; }
        public string VendorHexId { get; set; }
        public string ProductHexId { get; set; }
    }
    public class HidDeviceCapabilities
    {
        internal HidDeviceCapabilities(HIDP_CAPS capabilities)
        {
            Usage = capabilities.Usage;
            UsagePage = capabilities.UsagePage;
            InputReportByteLength = capabilities.InputReportByteLength;
            OutputReportByteLength = capabilities.OutputReportByteLength;
            FeatureReportByteLength = capabilities.FeatureReportByteLength;
            Reserved = capabilities.Reserved;
            NumberLinkCollectionNodes = capabilities.NumberLinkCollectionNodes;
            NumberInputButtonCaps = capabilities.NumberInputButtonCaps;
            NumberInputValueCaps = capabilities.NumberInputValueCaps;
            NumberInputDataIndices = capabilities.NumberInputDataIndices;
            NumberOutputButtonCaps = capabilities.NumberOutputButtonCaps;
            NumberOutputValueCaps = capabilities.NumberOutputValueCaps;
            NumberOutputDataIndices = capabilities.NumberOutputDataIndices;
            NumberFeatureButtonCaps = capabilities.NumberFeatureButtonCaps;
            NumberFeatureValueCaps = capabilities.NumberFeatureValueCaps;
            NumberFeatureDataIndices = capabilities.NumberFeatureDataIndices;

        }

        public short Usage { get; private set; }
        public short UsagePage { get; private set; }
        public short InputReportByteLength { get; private set; }
        public short OutputReportByteLength { get; private set; }
        public short FeatureReportByteLength { get; private set; }
        public short[] Reserved { get; private set; }
        public short NumberLinkCollectionNodes { get; private set; }
        public short NumberInputButtonCaps { get; private set; }
        public short NumberInputValueCaps { get; private set; }
        public short NumberInputDataIndices { get; private set; }
        public short NumberOutputButtonCaps { get; private set; }
        public short NumberOutputValueCaps { get; private set; }
        public short NumberOutputDataIndices { get; private set; }
        public short NumberFeatureButtonCaps { get; private set; }
        public short NumberFeatureValueCaps { get; private set; }
        public short NumberFeatureDataIndices { get; private set; }
    }
}
