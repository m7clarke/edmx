﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace fixedmx
{
    class Program
    {
        //http://blog.cincura.net/233202-changing-entity-names-i-e-removing-prefix-in-entity-frameworks-edmx-in-batch/

        static void Main(string[] args)
        {
            Console.WriteLine("ARGUMENTS: filename PREFIX_");
            TransformEDMXEntities(args[0], "temp.edmx", args[1]);

        }

        static void TransformEDMXEntities(string inputFile, string outputFile, string PREFIX="tbl")
        {
            XDocument xdoc = XDocument.Load(inputFile);

            const string CSDLNamespace = "http://schemas.microsoft.com/ado/2009/11/edm";
            const string MSLNamespace = "http://schemas.microsoft.com/ado/2009/11/mapping/cs";
            const string DesignerNamespace = "http://schemas.microsoft.com/ado/2009/11/edmx";
            XElement csdl = xdoc.Descendants(XName.Get("Schema", CSDLNamespace)).First();
            XElement msl = xdoc.Descendants(XName.Get("Mapping", MSLNamespace)).First();
            XElement designerDiagram = xdoc.Descendants(XName.Get("Designer", DesignerNamespace)).First();

            Func<string, string> transformation = (string input) =>
            {
                Regex re = new Regex(string.Format(@"{0}(\w+)", Regex.Escape(PREFIX)), RegexOptions.None);
                return re.Replace(input, new MatchEvaluator(
                    (Match m) =>
                    {
                        return m.Groups[1].Value;
                    }));
            };

            TransformCSDL(CSDLNamespace, csdl, transformation);
            TransformMSL(MSLNamespace, msl, transformation);
            TransformDesigner(DesignerNamespace, designerDiagram, transformation);

            XmlWriterSettings settings = new XmlWriterSettings { Indent = true };
            using (XmlWriter writer = XmlWriter.Create(outputFile, settings))
            {
                xdoc.WriteTo(writer);
            }
        }

        static void TransformDesigner(string DesignerNamespace, XElement designerDiagram, Func<string, string> transformation)
        {
            foreach (var item in designerDiagram.Elements(XName.Get("EntityTypeShape", DesignerNamespace)))
            {
                item.Attribute("EntityType").Value = transformation(item.Attribute("EntityType").Value);
            }
        }

        static void TransformMSL(string MSLNamespace, XElement msl, Func<string, string> transformation)
        {
            foreach (var entitySetMapping in msl.Element(XName.Get("EntityContainerMapping", MSLNamespace)).Elements(XName.Get("EntitySetMapping", MSLNamespace)))
            {
                entitySetMapping.Attribute("Name").Value = transformation(entitySetMapping.Attribute("Name").Value);
                foreach (var entityTypeMapping in entitySetMapping.Elements(XName.Get("EntityTypeMapping", MSLNamespace)))
                {
                    entityTypeMapping.Attribute("TypeName").Value = transformation(entityTypeMapping.Attribute("TypeName").Value);
                }
            }
        }

        static void TransformCSDL(string CSDLNamespace, XElement csdl, Func<string, string> transformation)
        {
            foreach (var entitySet in csdl.Element(XName.Get("EntityContainer", CSDLNamespace)).Elements(XName.Get("EntitySet", CSDLNamespace)))
            {
                entitySet.Attribute("Name").Value = transformation(entitySet.Attribute("Name").Value);
                entitySet.Attribute("EntityType").Value = transformation(entitySet.Attribute("EntityType").Value);
            }
            foreach (var associationSet in csdl.Element(XName.Get("EntityContainer", CSDLNamespace)).Elements(XName.Get("AssociationSet", CSDLNamespace)))
            {
                foreach (var end in associationSet.Elements(XName.Get("End", CSDLNamespace)))
                {
                    end.Attribute("EntitySet").Value = transformation(end.Attribute("EntitySet").Value);
                }
            }

            foreach (var entityType in csdl.Elements(XName.Get("EntityType", CSDLNamespace)))
            {
                entityType.Attribute("Name").Value = transformation(entityType.Attribute("Name").Value);
            }
            foreach (var association in csdl.Elements(XName.Get("Association", CSDLNamespace)))
            {
                foreach (var end in association.Elements(XName.Get("End", CSDLNamespace)))
                {
                    end.Attribute("Type").Value = transformation(end.Attribute("Type").Value);
                }
            }
        }
    }
}
