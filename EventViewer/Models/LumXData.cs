namespace EventViewer.Models
{
    public class LumXData
    {
        public string DeviceSerialNumber { get; set; }
        public string DeviceType { get; set; }
        public string LocalSessionId { get; set; }

        public override string ToString()
        {
            return LocalSessionId + DeviceType + DeviceSerialNumber;
        }
    }
}
