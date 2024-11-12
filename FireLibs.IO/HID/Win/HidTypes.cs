/*
 * Modified by: Oscar-Wohlfarhrt - Github: https://github.com/Oscar-Wohlfarhrt
 * Original from: jhebb and JoshWobbles - Github: https://github.com/InputMapper/Dualshock4
*/

using static FireLibs.IO.NativeMethods;

namespace FireLibs.IO.HID.Win
{
    /// <summary>
    /// Status enumeration for Read/Write operations
    /// </summary>
    public enum ReadWriteStatus
    {
#pragma warning disable CS1591 // Falta el comentario XML para el tipo o miembro visible públicamente
        Success = 0,
        WaitTimedOut = 1,
        WaitFail = 2,
        NoData = 3,
        Error = 4,
        NotConnected = 5
#pragma warning restore CS1591 // Falta el comentario XML para el tipo o miembro visible públicamente
    }
    /// <summary>
    /// HidInfo class to store HidDevice basic information
    /// </summary>
    public class HidInfo
    {
        /// <summary>
        /// Gets/Sets the Hid Device path
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// Gets/Sets the Hid Device name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets/Sets the Hid Device id (can be used to identify the device when the name is not enough)
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Gets/Sets the Hid Device vendor id
        /// </summary>
        public int VendorId { get; set; }
        /// <summary>
        /// Gets/Sets the Hid Device product id
        /// </summary>
        public int ProductId { get; set; }
        /// <summary>
        /// HidInfo class constructor
        /// </summary>
        /// <param name="path">The path of the Hid Device</param>
        /// <param name="name">The friendly name of the Hid Device</param>
        public HidInfo(string path, string name) : this(path, name, path, 0, 0) { }
        /// <summary>
        /// HidInfo class constructor
        /// </summary>
        /// <param name="path">The path of the Hid Device</param>
        /// <param name="name">The friendly name of the Hid Device</param>
        /// <param name="vendorId">The vendor id of the Hid Device</param>
        /// <param name="productId">The product id of the Hid Device</param>
        public HidInfo(string path, string name, int vendorId, int productId) : this(path, name, path, vendorId, productId) { }
        /// <summary>
        /// HidInfo class constructor
        /// </summary>
        /// <param name="path">The path of the Hid Device</param>
        /// <param name="name">The friendly name of the Hid Device</param>
        /// <param name="id">The id for the Hid Device</param>
        /// <param name="vendorId">The vendor id of the Hid Device</param>
        /// <param name="productId">The product id of the Hid Device</param>
        public HidInfo(string path, string name, string id, int vendorId, int productId)
        {
            Path = path;
            Name = name;
            Id = id;
            VendorId = vendorId;
            ProductId = productId;
        }
    }
    /// <summary>
    /// Hid Attributes class containing especific device information
    /// </summary>
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
        /// <summary>
        /// Vendor id of the device
        /// </summary>
        public int VendorId { get; private set; }
        /// <summary>
        /// Product id of the device
        /// </summary>
        public int ProductId { get; private set; }
        /// <summary>
        /// Version number of the device
        /// </summary>
        public int Version { get; private set; }
        /// <summary>
        /// Vendor id of the device as an Hexadecimal string
        /// </summary>
        public string VendorHexId { get; set; }
        /// <summary>
        /// Product id of the device as an Hexadecimal string
        /// </summary>
        public string ProductHexId { get; set; }
    }
    /// <summary>
    /// Hid Capabilities class containing especific device information
    /// </summary>
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

#pragma warning disable CS1591 // Falta el comentario XML para el tipo o miembro visible públicamente
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
#pragma warning restore CS1591 // Falta el comentario XML para el tipo o miembro visible públicamente
    }
}
