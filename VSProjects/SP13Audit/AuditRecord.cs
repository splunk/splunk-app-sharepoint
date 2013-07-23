using System;
using System.Text;

namespace Splunk.SharePoint2013.Audit
{
    class AuditRecord
    {
        public int CheckSum { get; set; }
        public Guid FarmId { get; set; }
        public Guid SiteId { get; set; }
        public Guid ItemId { get; set; }
        public short ItemType { get; set; }
        public int UserId { get; set; }
        public string DocLocation { get; set; }
        public byte LocationType { get; set; }
        public DateTime Occurred { get; set; }
        public int Event { get; set; }
        public string EventName { get; set; }
        public byte EventSource { get; set; }
        public string SourceName { get; set; }
        public string EventData { get; set; }
        public string UserName { get; set; }

        public string ToLogString(bool useNewLine)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(Occurred.ToString("R"));
            sb.AppendLine(string.Format("CheckSum={0}", CheckSum));
            sb.AppendLine(string.Format("FarmId={0}", FarmId));
            sb.AppendLine(string.Format("SiteId={0}", SiteId));
            sb.AppendLine(string.Format("ItemId={0}", ItemId));
            sb.AppendLine(string.Format("ItemType={0}", ItemType));
            sb.AppendLine(string.Format("UserId={0}", UserId));
            sb.AppendLine(string.Format("DocLocation=\"{0}\"", DocLocation));
            sb.AppendLine(string.Format("LocationType={0}", LocationType));
            sb.AppendLine(string.Format("Event={0}", Event));
            sb.AppendLine(string.Format("EventName=\"{0}\"", EventName));
            sb.AppendLine(string.Format("EventSource={0}", EventSource));
            sb.AppendLine(string.Format("SourceName=\"{0}\"", SourceName));
            sb.AppendLine(string.Format("EventData=\"{0}\"", EventData));
            sb.AppendLine(string.Format("UserName=\"{0}\"", UserName));

            if (!useNewLine)
            {
                sb.Replace("\r\n", " ");
            }

            return sb.ToString();
        }
    }
}
