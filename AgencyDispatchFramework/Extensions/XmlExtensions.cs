using System;
using System.Xml;

namespace AgencyDispatchFramework.Extensions
{
    public static class XmlExtensions
    {
        /// <summary>
        /// Gets the full path to the specified <see cref="XmlNode"/>
        /// </summary>
        public static string GetFullPath(this XmlNode node)
        {
            string path = node.Name;
            XmlNode search = null;

            // Get up until ROOT
            while ((search = node.ParentNode).NodeType != XmlNodeType.Document)
            {
                path = search.Name + " > " + path; // Add to path
                node = search;
            }

            return path;
        }

        /// <summary>
        /// Returns the value for the attribute with the specified name.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="name"></param>
        /// <returns>
        /// The value of the specified attribute. An empty string is returned if a matching attribute 
        /// is not found or if the attribute does not have a specified or default value.
        /// </returns>
        public static string GetAttribute(this XmlNode node, string name)
        {
            if (node.Attributes != null)
            {
                return node.Attributes[name]?.Value ?? null;
            }

            return null;
        }

        /// <summary>
        /// Returns the value for the attribute with the specified name.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="name"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>
        /// The value of the specified attribute, converted to the specified type of T. 
        /// Default(T) if a matching attribute is not found or if the attribute does not have a specified value.
        /// </returns>
        public static T GetAttribute<T>(this XmlNode node, string name) where T : IConvertible
        {
            if (node.Attributes != null)
            {
                var value = node.Attributes[name]?.Value;
                if (String.IsNullOrEmpty(value))
                {
                    return default(T);
                }

                return (T)Convert.ChangeType(value, typeof(T));
            }

            return default(T);
        }

        /// <summary>
        /// Returns the value for the attribute with the specified name.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="name"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>
        /// The value of the specified attribute, converted to the specified type of T. 
        /// Default(T) if a matching attribute is not found or if the attribute does not have a specified value.
        /// </returns>
        public static bool TryGetAttribute(this XmlNode node, string name, out string value)
        {
            var val = node.Attributes[name]?.Value;
            if (String.IsNullOrEmpty(val))
            {
                value = null;
                return false;
            }
            else
            {
                value = val;
                return true;
            }
        }

        /// <summary>
        /// Returns the value for the attribute with the specified name.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="name"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>
        /// The value of the specified attribute, converted to the specified type of T. 
        /// Default(T) if a matching attribute is not found or if the attribute does not have a specified value.
        /// </returns>
        public static bool TryGetAttribute<T>(this XmlNode node, string name, out T value) where T : IConvertible
        {
            if (node.Attributes != null)
            {
                var val = node.Attributes[name]?.Value;
                if (String.IsNullOrEmpty(val))
                {
                    value = default(T);
                    return false;
                }

                try
                {
                    value = (T)Convert.ChangeType(val, typeof(T));
                    return true;
                }
                catch (Exception)
                {
                    // Don't worry
                }
            }

            value = default(T);
            return false;
        }

        /// <summary>
        /// Determines whether the current node has the specified attribute.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="name"></param>
        /// <returns>
        /// true if the current node has the specified attribute; otherwise, false.
        /// </returns>
        public static bool HasAttribute(this XmlNode node, string name)
        {
            return !String.IsNullOrEmpty(node.GetAttribute(name));
        }

        /// <summary>
        /// Ensures a node path exists in an <see cref="XmlDocument"/> by creating it if
        /// it does not exist.
        /// </summary>
        /// <param name="document">The document</param>
        /// <param name="rootName">The root node name in the XmlDocument</param>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static XmlNode EnsureXmlNodePathExists(this XmlDocument document, string rootName, params string[] paths)
        {
            // Walk
            XmlNode node = document.SelectSingleNode(rootName);
            foreach (string path in paths)
            {
                XmlNode child = node.SelectSingleNode(path);
                if (child != null)
                {
                    node = child;
                }
                else
                {
                    child = document.CreateElement(path);
                    node.AppendChild(child);
                    node = child;
                }
            }

            return node;
        }

        /// <summary>
        /// Closes an element tag with short hand if there is no inner text
        /// </summary>
        /// <param name="node"></param>
        public static void CloseElementTagShortIfEmpty(this XmlElement node)
        {
            if (String.IsNullOrWhiteSpace(node.InnerText))
            {
                node.IsEmpty = true;
            }
        }
    }
}
