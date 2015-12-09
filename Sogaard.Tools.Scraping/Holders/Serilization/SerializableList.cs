using System.Collections.Generic;
using System.Xml.Serialization;

namespace Sogaard.Tools.Scraping.Holders.Serilization
{
    public class SerializableList<T> : List<T>, IXmlSerializable
    {
        #region IXmlSerializable Members
        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            XmlSerializer valueSerializer = new XmlSerializer(typeof(T));

            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();

            if (wasEmpty)
                return;

            while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
            {
                reader.ReadStartElement("item");
                
                T value = (T)valueSerializer.Deserialize(reader);

                this.Add(value);

                reader.ReadEndElement();
                reader.MoveToContent();
            }
            reader.ReadEndElement();
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            XmlSerializer valueSerializer = new XmlSerializer(typeof(T));

            foreach (T value in this)
            {
                writer.WriteStartElement("item");
                valueSerializer.Serialize(writer, value);
                writer.WriteEndElement();
            }
        }
        #endregion
    }
}