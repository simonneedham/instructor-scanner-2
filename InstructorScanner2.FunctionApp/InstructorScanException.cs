using System;
using System.Runtime.Serialization;

namespace InstructorScanner2.FunctionApp
{
    [Serializable]
    public class InstructorScanException : Exception
    {
        public InstructorScanException() { }
        public InstructorScanException(string message) : base(message) { }
        public InstructorScanException(string message, Exception inner) : base(message, inner) { }
        protected InstructorScanException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
