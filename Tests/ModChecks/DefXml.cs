using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Mono.Cecil;

namespace Azrael.ModChecks
{
    static class DefXml
    {
        public static void CheckFields(string repo, RefPack pack, List<string> errors)
        {
            foreach (string folder in Suite.ModFolders)
            {
                foreach (string defs in Suite.DefFolders(repo, folder))
                {
                    foreach (string file in Directory.GetFiles(defs, "*.xml", SearchOption.AllDirectories))
                    {
                        string rel = Suite.Rel(repo, file).Replace('\\', '/');
                        if (rel.IndexOf("/Patches/", StringComparison.OrdinalIgnoreCase) >= 0)
                            continue;
                        CheckFile(file, rel, pack, errors);
                    }
                }
            }
        }

        static void CheckFile(string file, string rel, RefPack pack, List<string> errors)
        {
            XDocument doc;
            try
            {
                doc = XDocument.Load(file, LoadOptions.SetLineInfo);
            }
            catch
            {
                return;
            }

            IEnumerable<XElement> els = doc.Root?.Name.LocalName == "Defs"
                ? doc.Root.Elements()
                : (doc.Root != null ? new[] { doc.Root } : Array.Empty<XElement>());

            foreach (XElement el in els)
            {
                if (el.NodeType != XmlNodeType.Element)
                    continue;
                ValidateDef(el, rel, pack, errors);
            }
        }

        static void ValidateDef(XElement el, string rel, RefPack pack, List<string> errors)
        {
            string typeName = (string)el.Attribute("Class") ?? el.Name.LocalName;
            TypeDefinition type = pack.FindType(typeName);
            string defName = (string)el.Element("defName") ?? el.Name.LocalName;
            if (type == null)
            {
                if (HasMayRequire(el))
                    return;
                errors.Add(Loc(rel, el, defName) + " unknown def type " + typeName);
                return;
            }

            foreach (XElement child in el.Elements())
                ValidateNode(child, type, rel, defName, pack, errors);
        }

        static void ValidateNode(
            XElement el, TypeDefinition type, string rel, string defName, RefPack pack, List<string> errors)
        {
            string tag = el.Name.LocalName;
            if (tag == "li")
            {
                string className = (string)el.Attribute("Class");
                TypeDefinition itemType = className != null ? pack.FindType(className) : type;
                if (itemType == null)
                {
                    if (HasMayRequire(el) || className != null)
                        return;
                    errors.Add(Loc(rel, el, defName) + " unknown list item type");
                    return;
                }
                foreach (XElement child in el.Elements())
                    ValidateNode(child, itemType, rel, defName, pack, errors);
                return;
            }

            if (!pack.TryGetField(type, tag, out TypeReference fieldType))
            {
                if (HasMayRequire(el))
                    return;
                errors.Add(Loc(rel, el, defName) + " unknown field <" + tag + ">");
                return;
            }

            TypeReference listEl = pack.ListElementType(fieldType);
            if (listEl != null)
            {
                TypeDefinition listType = pack.Resolve(listEl);
                bool anyLi = false;
                bool anyFieldChild = false;
                foreach (XElement child in el.Elements())
                {
                    if (child.Name.LocalName == "li")
                    {
                        anyLi = true;
                        ValidateNode(child, listType ?? type, rel, defName, pack, errors);
                    }
                    else if (listType != null && pack.TryGetField(listType, child.Name.LocalName, out _))
                    {
                        anyFieldChild = true;
                        ValidateNode(child, listType, rel, defName, pack, errors);
                    }
                }
                // StatModifier / ThingDefCountClass shorthand: <MaxHitPoints>80</MaxHitPoints>
                if (!anyLi && !anyFieldChild)
                    return;
                return;
            }

            TypeDefinition nested = pack.Resolve(fieldType);
            if (nested == null || IsPrimitiveLike(fieldType))
                return;

            string nestedClass = (string)el.Attribute("Class");
            if (nestedClass != null)
            {
                TypeDefinition switched = pack.FindType(nestedClass);
                if (switched != null)
                    nested = switched;
                else if (HasMayRequire(el))
                    return;
            }

            foreach (XElement child in el.Elements())
                ValidateNode(child, nested, rel, defName, pack, errors);
        }

        static bool IsPrimitiveLike(TypeReference tr)
        {
            if (tr == null)
                return true;
            if (!tr.IsValueType && tr.Name != "String" && tr.Name != "Type")
                return false;
            return tr.Name != "GraphicData"
                && !tr.Name.StartsWith("CompProperties", StringComparison.Ordinal)
                && !tr.Name.EndsWith("Def", StringComparison.Ordinal);
        }

        static bool HasMayRequire(XElement el)
        {
            return el.Attribute("MayRequire") != null
                || el.Attribute("MayRequireAnyOf") != null;
        }

        static string Loc(string rel, XElement el, string defName)
        {
            var info = (IXmlLineInfo)el;
            int line = info.HasLineInfo() ? info.LineNumber : 0;
            return rel + ":" + line + " [" + defName + "]";
        }
    }
}
