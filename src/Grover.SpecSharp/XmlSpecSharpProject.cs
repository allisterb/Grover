using System.Xml.Serialization;
using System.Xml;
namespace Grover.SpecSharp
{
    public class XmlSpecSharpProject : SpecSharpProject
    {
        public XmlSpecSharpProject(string fileName) : base(fileName)
        {
            XmlSerializer ser = new XmlSerializer(typeof(VisualStudioProject));
            using (XmlReader reader = XmlReader.Create(fileName))
            {
                Model = (VisualStudioProject?) ser.Deserialize(reader);
                Initialized = Model is not null;
            }
        }

        protected VisualStudioProject? Model { get; init; }
    }

    
}
