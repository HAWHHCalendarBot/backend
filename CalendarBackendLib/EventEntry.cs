﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CalendarBackendLib
{
    public enum EventStatus { Confirmed, Tentative, Cancelled }

    public class EventEntry : ICloneable
    {
        public string Name { get; set; }
        public EventStatus Status { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }

        public string PrettyName { get; set; }


        public DateTime StartDateTime => ParseTimestampStringToDateTime(StartTime);
        public DateTime EndDateTime => ParseTimestampStringToDateTime(EndTime);

        public TimeSpan Duration => EndDateTime - StartDateTime;
        public string EventNameOnFilesystem => GetEventnameOnFilesystem(Name);
        public string Filename => GetFilename(Name);

        [Obsolete("Use parameterized Constructor")]
        public EventEntry()
        { }

        public EventEntry(string name, DateTime startTime, DateTime endTime, EventStatus status = EventStatus.Confirmed)
        {
            Name = name;
            Status = status;
            StartTime = startTime.ToString("yyyy-MM-ddTHH\\:mm\\:sszzz");
            EndTime = endTime.ToString("yyyy-MM-ddTHH\\:mm\\:sszzz");
        }

        public EventEntry(string name, DateTime startTime, TimeSpan duration, EventStatus status = EventStatus.Confirmed)
            : this(name, startTime, startTime + duration, status)
        { }

        public static DateTime ParseTimestampStringToDateTime(string timestamp)
        {
            if (timestamp.StartsWith('/'))
            {
                var millisString = new Regex(@"\((\d+)").Match(timestamp).Groups[1].Value;
                return DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(millisString)).DateTime;
            }

            return DateTime.Parse(timestamp);
        }

        public static string GetEventnameOnFilesystem(string eventname)
        {
            return eventname.Replace('/', '-');
        }

        public static string GetFilename(string eventname)
        {
            return GetEventnameOnFilesystem(eventname) + ".json";
        }

        public bool Equals(EventEntry other)
        {
            if (other == null)
            {
                return false;
            }

            if (Name != other.Name) return false;
            if (Status != other.Status) return false;
            if (StartTime != other.StartTime) return false;
            if (EndTime != other.EndTime) return false;
            if (Description != other.Description) return false;
            if (Location != other.Location) return false;

            return true;
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            return Equals(obj as EventEntry);
        }

        public override int GetHashCode()
        {
            return StartTime.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Name, StartTime);
        }

        public object Clone()
        {
            return new EventEntry(Name, StartDateTime, EndDateTime, Status)
            {
                Description = Description,
                Location = Location,
                PrettyName = PrettyName
            };
        }
    }
}
