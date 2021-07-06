using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace EventViewer.Exceptions
{
    public enum EventViewerError
    {
        INVALID_CREDENTIALS,
        LIMIT_EXCEEDED,
        NOT_FOUND
    }

    [Serializable()]
    public class EventViewerException : System.Exception
    {
        public EventViewerError ErrorCode { get; set; }

        public EventViewerException() : base() { }
        public EventViewerException(EventViewerError error, string message) : base(message) { ErrorCode = error; }
        public EventViewerException(EventViewerError error, string message, System.Exception inner) : base(message, inner) { ErrorCode = error; }

        // A constructor is needed for serialization when an exception propagates from a remoting server to the client. 
        protected EventViewerException(SerializationInfo info, StreamingContext context) { }
    }
}
