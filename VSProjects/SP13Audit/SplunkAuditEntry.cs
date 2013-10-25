using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;

namespace Splunk.SharePoint2013.Audit
{
    /// <summary>
    /// Over-ride of the SPAuditEntry, which is sealed so we cannot sub-class it.
    /// We want to add certain facilities to this class for better auditing.
    /// </summary>
    internal class SplunkAuditEntry
    {
        private SPAuditEntry auditEntry;
        private string _username = null;
        private SPSite _site;

        /// <summary>
        /// Gets the ID of the app principal who caused the event
        /// </summary>
        public Nullable<Int32> AppPrincipalId 
        { 
            get { return auditEntry.AppPrincipalId;  } 
        }

        /// <summary>
        /// Gets the Location of an audited object at the time of the audited event
        /// </summary>
        public string DocLocation 
        { 
            get { return auditEntry.DocLocation; } 
        }

        /// <summary>
        /// Gets a value that identifies the type of event
        /// </summary>
        public SPAuditEventType Event 
        { 
            get { return auditEntry.Event;  } 
        }

        /// <summary>
        /// Gets data, in XML markup, that is specific to the type of event identified in the Event property
        /// </summary>
        public string EventData 
        { 
            get { return auditEntry.EventData; }
        }

        /// <summary>
        /// Gets the name of the type of a custom audited event.
        /// </summary>
        public string EventName
        { 
            get { return auditEntry.EventName; }
        }

        /// <summary>
        /// A value that indicates whether the event occurred as a result of user action in the SharePoint foundation UI or programatically.
        /// </summary>
        public SPAuditEventSource EventSource
        { 
            get { return auditEntry.EventSource; }
        }

        /// <summary>
        /// Gets the SharePOint Foundation GUID of the audited object.
        /// </summary>
        public Guid ItemId
        { 
            get { return auditEntry.ItemId; }
        }

        /// <summary>
        /// Gets the type of object whose event is represented by the SPAuditEntry
        /// </summary>
        public SPAuditItemType ItemType
        { 
            get { return auditEntry.ItemType; }
        }

        /// <summary>
        /// Gets a value that indicates where the event occurred.
        /// </summary>
        public SPAuditLocationType LocationType 
        { 
            get { return auditEntry.LocationType; }
        }

        /// <summary>
        /// Gets the IP address of the computer that initiated the event
        /// </summary>
        public string MachineIP 
        { 
            get { return auditEntry.MachineIP; }
        }

        /// <summary>
        /// Gets the name of the computer that initiated the event
        /// </summary>
        public string MachineName 
        { 
            get { return auditEntry.MachineName; }
        }

        /// <summary>
        /// Gets the date and time of the audited event.  We convert this to a fully-qualified
        /// DateTime object before returning it.
        /// </summary>
        public DateTime Occurred 
        { 
            get { return new DateTime(auditEntry.Occurred.Ticks, DateTimeKind.Utc); }
        }

        /// <summary>
        /// Gets the SharePoint Foundation GUID of the site collection
        /// </summary>
        public Guid SiteId
        { 
            get { return auditEntry.SiteId; }
        }

        /// <summary>
        /// Gets the name of the application that caused the event and wrote the SPAuditEntry data to the SharePoint database
        /// </summary>
        public string SourceName
        { 
            get { return auditEntry.SourceName; }
        }

        /// <summary>
        /// Gets the ID of the user who caused the event
        /// </summary>
        public int UserId
        { 
            get { return auditEntry.UserId; }
        }

        /// <summary>
        /// Gets the Username of the user who caused the event
        /// </summary>
        public string UserName
        {
            get
            {
                if (_username == null)
                {
                    using (SPWeb web = _site.OpenWeb())
                    {
                        SPUser user = web.AllUsers.GetByID(auditEntry.UserId);
                        if (user != null)
                        {
                            _username = user.LoginName;
                        }
                        else
                        {
                            _username = "!UNKNOWN";
                        }
                    }
                }
                return _username;
            }
        }

        /// <summary>
        /// Gets the Guid of the Local Farm that this event was created on.
        /// </summary>
        public Guid FarmId { get; private set; }

        /// <summary>
        /// Constructor - create a new SplunkAuditEntry based on an SPAuditEntry
        /// </summary>
        /// <param name="localFarm">The farm where this audit entry was created</param>
        /// <param name="site">The site collection where this audit entry was created</param>
        /// <param name="auditEntry">The audit entry</param>
        public SplunkAuditEntry(SPFarm localFarm, SPSite site, SPAuditEntry auditEntry)
        {
            FarmId = localFarm.Id;
            this._site = site;
            this.auditEntry = auditEntry;
        }

        /// <summary>
        /// Convert the object to a suitable output format.
        /// </summary>
        /// <returns>A string representation of the output</returns>
        public override string ToString()
        {
            List<string> elements = new List<string>();

            elements.Add(string.Format("{0}", Occurred.ToString("u")));
            elements.Add(string.Format("FarmId={0}", FarmId));
            elements.Add(string.Format("SiteId={0}", SiteId));
            elements.Add(string.Format("ItemId={0}", ItemId));
            if (AppPrincipalId != null) 
                elements.Add(string.Format("AppPrincipalId={0}", AppPrincipalId));
            elements.Add(string.Format("ItemType={0}", (int)(ItemType)));
            elements.Add(string.Format("DocLocation=\"{0}\"", DocLocation));
            elements.Add(string.Format("LocationType={0}", (int)(LocationType)));
            elements.Add(string.Format("SourceName=\"{0}\"", SourceName));
            elements.Add(string.Format("UserId={0}", UserId));
            elements.Add(string.Format("UserName=\"{0}\"", UserName));
            elements.Add(string.Format("EventSource={0}", (int)(EventSource)));
            elements.Add(string.Format("EventName=\"{0}\"", EventName));
            elements.Add(string.Format("EventType={0}", (int)(Event)));
            elements.Add(string.Format("EventData:\"{0}\"", EventData));
            if (MachineIP != null && MachineIP.Length > 0)
                elements.Add(string.Format("Machine:{ip:\"{0}\",name:\"{1}\"", MachineIP, MachineName));

            return string.Join("\n", elements);
        }
    }
}
