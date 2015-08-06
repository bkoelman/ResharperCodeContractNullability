using System.IO;
using System.Xml.Linq;
using CodeContractNullability.ExternalAnnotations.Storage;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;

namespace CodeContractNullability.ExternalAnnotations
{
    /// <summary>
    /// Parses the contents of a single external annotations xml file.
    /// </summary>
    public class ExternalAnnotationDocumentParser
    {
        public void ProcessDocument([NotNull] TextReader reader, [NotNull] ExternalAnnotationsMap result)
        {
            Guard.NotNull(reader, nameof(reader));
            Guard.NotNull(result, nameof(result));

            XElement assemblyElement = XDocument.Load(reader).Element("assembly");
            if (assemblyElement != null)
            {
                // Known limitation: we are not entirely correct here, by ignoring assembly info.
                // You'll run into this, for example, with the next block of code:
                //
                //    public class MyEnumerator : IEnumerator
                //    {
                //        public bool MoveNext() { throw new System.NotImplementedException(); }
                //
                //        public void Reset() { }
                //
                //        [CanBeNull]
                //        public object Current { get; }
                //    }
                //
                // When you set project properties to target .NET Framework v4.5, Resharper is fine with
                // the [CanBeNull]. But if you switch to target .NET Framework v2, then Resharper grays 
                // out the [CanBeNull], with hover message "Base declaration has the same annotation".
                // This is because the external annotation file "2.0.0.0.Interfaces.Nullness.Gen.xml"
                // contains the following snapshot:
                //
                //    <?xml version="1.0" encoding="utf-8"?>
                //    <assembly name="mscorlib, Version=2.0.0.0">
                //      <member name="P:System.Collections.IEnumerator.Current">
                //        <attribute ctor="M:JetBrains.Annotations.CanBeNullAttribute.#ctor" />
                //      </member>
                //    </assembly>
                //
                // But when targeting the .NET Framework v4.5, mscorlib v4.0.0.0 is used, so this snapshot
                // does not apply. To support this, we need to add assembly info to our data structure
                // for each symbol. That makes the dataset grow a lot, taking longer to load/save.

                foreach (XElement memberElement in assemblyElement.Elements("member"))
                {
                    string memberType = "?";
                    string memberName = memberElement.Attribute("name").Value;
                    if (memberName.Length > 2 && memberName[1] == ':')
                    {
                        memberType = memberName[0].ToString();
                        memberName = memberName.Substring(2);
                    }

                    MemberNullabilityInfo memberInfo = result.ContainsKey(memberName)
                        ? result[memberName]
                        : new MemberNullabilityInfo(memberType);

                    foreach (XElement childElement in memberElement.Elements())
                    {
                        if (childElement.Name == "parameter")
                        {
                            string parameterName = childElement.Attribute("name").Value;
                            foreach (XElement attributeElement in childElement.Elements("attribute"))
                            {
                                if (ElementHasNullabilityDefinition(attributeElement))
                                {
                                    memberInfo.ParametersNullability[parameterName] = true;
                                }
                            }
                        }
                        if (childElement.Name == "attribute")
                        {
                            if (ElementHasNullabilityDefinition(childElement))
                            {
                                memberInfo.HasNullabilityDefined = true;
                            }
                        }
                    }

                    result[memberName] = memberInfo;
                }
            }
        }

        private static bool ElementHasNullabilityDefinition([NotNull] XElement element)
        {
            string attributeName = element.Attribute("ctor").Value;
            return attributeName == "M:JetBrains.Annotations.NotNullAttribute.#ctor" ||
                attributeName == "M:JetBrains.Annotations.CanBeNullAttribute.#ctor";
        }
    }
}