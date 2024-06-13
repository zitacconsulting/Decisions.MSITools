using DecisionsFramework.Design.Properties.Attributes;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.Service.Debugging.DebugData;
using System.Runtime.Serialization;


namespace Zitac.MSIToolsWindows;

        [DataContract]
        public class ByteArray : IDebuggerJsonProvider
    {
        [DataMember]
        [WritableValue]
        public byte[]? Content { get; set; }

        public object GetJsonDebugView()
        {
                return new
                {
                   Content = Content.Length + " bytes"
                };
        }
    }