using System.IO;
using System.Xml.Linq;

namespace SkylineNModFilter
{
    internal sealed class PeptideRecord
    {
        private PeptideRecord(XElement element, string modifiedSequence)
        {
            Element = element;
            ModifiedSequence = modifiedSequence;
        }

        public XElement Element { get; private set; }
        public string ModifiedSequence { get; private set; }

        public static PeptideRecord FromElement(XElement element)
        {
            var attribute = element == null ? null : element.Attribute("modified_sequence");
            if (attribute == null || string.IsNullOrWhiteSpace(attribute.Value))
                throw new InvalidDataException("A peptide is missing its modified_sequence attribute.");

            return new PeptideRecord(element, attribute.Value);
        }
    }
}
