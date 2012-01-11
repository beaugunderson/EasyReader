using System;
using System.Xml;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Windows.Data.Xml.Dom;
using Windows.UI.Xaml;

using MarkupConverter.Metro;
using MarkupConverter.Metro.Hacks;

namespace MarkupConverter
{
    /// <summary>
    /// HtmlToXamlConverter is a static class that takes an HTML string
    /// and converts it into XAML
    /// </summary>
    public static class HtmlToXamlConverter
    {
        // ---------------------------------------------------------------------
        //
        // Internal Methods
        //
        // ---------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Converts an html string into xaml string.
        /// </summary>
        /// <param name="htmlString">
        /// Input html which may be badly formatted xml.
        /// </param>
        /// <param name="asFlowDocument">
        /// true indicates that we need a FlowDocument as a root element;
        /// false means that Section or Span elements will be used
        /// dependeing on StartFragment/EndFragment comments locations.
        /// </param>
        /// <returns>
        /// Well-formed xml representing XAML equivalent for the input html string.
        /// </returns>
        public static string ConvertHtmlToXaml(string htmlString, bool asFlowDocument)
        {
            // Create well-formed Xml from Html string
            XmlElement htmlElement = HtmlParser.ParseHtml(htmlString);

            // Decide what name to use as a root
            string rootElementName = asFlowDocument ? HtmlToXamlConverter.Xaml_FlowDocument : HtmlToXamlConverter.Xaml_Section;

            // Create an XmlDocument for generated xaml
            XmlDocument xamlTree = new XmlDocument();

            XmlElement xamlFlowDocumentElement = xamlTree.CreateElementNS(_xamlNamespace, rootElementName);

            // Source context is a stack of all elements - ancestors of a parentElement
            var sourceContext = new List<IXmlNode>(10);

            // Clear fragment parent
            InlineFragmentParentElement = null;

            // convert root html element
            AddBlock(xamlFlowDocumentElement, htmlElement, new Dictionary<string, string>(), sourceContext);

            // In case if the selected fragment is inline, extract it into a separate Span wrapper
            if (!asFlowDocument)
            {
                xamlFlowDocumentElement = ExtractInlineFragment(xamlFlowDocumentElement) as XmlElement;
            }

            // Return a string representing resulting Xaml
            xamlFlowDocumentElement.SetAttribute("xml:space", "preserve");

            string xaml = xamlFlowDocumentElement.GetXml();

            return xaml;
        }

        /// <summary>
        /// Returns a value for an attribute by its name (ignoring casing)
        /// </summary>
        /// <param name="element">
        /// XmlElement in which we are trying to find the specified attribute
        /// </param>
        /// <param name="attributeName">
        /// String representing the attribute name to be searched for
        /// </param>
        /// <returns></returns>
        public static string GetAttribute(IXmlNode element, string attributeName)
        {
            attributeName = attributeName.ToLower();

            for (int i = 0; i < element.Attributes.Count; i++)
            {
                if (element.Attributes[i].NodeName.ToLower() == attributeName)
                {
                    return (string)element.Attributes[i].NodeValue;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns string extracted from quotation marks
        /// </summary>
        /// <param name="value">
        /// String representing value enclosed in quotation marks
        /// </param>
        internal static string UnQuote(string value)
        {
            if (value.StartsWith("\"") && value.EndsWith("\"") || value.StartsWith("'") && value.EndsWith("'"))
            {
                value = value.Substring(1, value.Length - 2).Trim();
            }
            return value;
        }

        #endregion Internal Methods

        // ---------------------------------------------------------------------
        //
        // Private Methods
        //
        // ---------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Analyzes the given htmlElement expecting it to be converted
        /// into some of xaml Block elements and adds the converted block
        /// to the children collection of xamlParentElement.
        /// 
        /// Analyzes the given XmlElement htmlElement, recognizes it as some HTML element
        /// and adds it as a child to a xamlParentElement.
        /// In some cases several following siblings of the given htmlElement
        /// will be consumed too (e.g. LIs encountered without wrapping UL/OL, 
        /// which must be collected together and wrapped into one implicit List element).
        /// </summary>
        /// <param name="xamlParentElement">
        /// Parent xaml element, to which new converted element will be added
        /// </param>
        /// <param name="htmlElement">
        /// Source html element subject to convert to xaml.
        /// </param>
        /// <param name="inheritedProperties">
        /// Properties inherited from an outer context.
        /// </param>
        /// <param name="sourceContext"></param>
        /// <returns>
        /// Last processed html node. Normally it should be the same htmlElement
        /// as was passed as a paramater, but in some irregular cases
        /// it could one of its following siblings.
        /// The caller must use this node to get to next sibling from it.
        /// </returns>
        private static IXmlNode AddBlock(IXmlNode xamlParentElement, IXmlNode htmlNode, Dictionary<string, string> inheritedProperties, List<IXmlNode> sourceContext)
        {
            if (htmlNode.NodeType == NodeType.CommentNode)
            {
                DefineInlineFragmentParent(htmlNode, null);
            }
            else if (htmlNode.NodeType == NodeType.TextNode)
            {
                htmlNode = AddImplicitParagraph(xamlParentElement, htmlNode, inheritedProperties, sourceContext);
            }
            else if (htmlNode is XmlElement)
            {
                // Identify element name
                var htmlElement = htmlNode;

                string htmlElementName = (string)htmlElement.LocalName; // Keep the name case-sensitive to check xml names
                string htmlElementNamespace = (string)htmlElement.NamespaceUri;

                if (htmlElementNamespace != HtmlParser.XhtmlNamespace)
                {
                    // Non-html element. skip it
                    // Isn't it too agressive? What if this is just an error in html tag name?
                    // TODO: Consider skipping just a wparrer in recursing into the element tree,
                    // which may produce some garbage though coming from xml fragments.
                    return htmlElement;
                }

                // Put source element to the stack
                sourceContext.Add(htmlElement);

                // Convert the name to lowercase, because html elements are case-insensitive
                htmlElementName = htmlElementName.ToLower();

                // Switch to an appropriate kind of processing depending on html element name
                switch (htmlElementName)
                {
                    // Sections:
                    case "html":
                    case "body":
                    case "div":
                    case "form": // not a block according to xhtml spec
                    case "pre": // Renders text in a fixed-width font
                    case "blockquote":
                    case "caption":
                    case "center":
                    case "cite":
                        AddSection(xamlParentElement, htmlElement, inheritedProperties, sourceContext);
                        break;

                    // Paragraphs:
                    case "p":
                    case "h1":
                    case "h2":
                    case "h3":
                    case "h4":
                    case "h5":
                    case "h6":
                    case "nsrtitle":
                    case "textarea":
                    case "dd": // ???
                    case "dl": // ???
                    case "dt": // ???
                    case "tt": // ???
                        AddParagraph(xamlParentElement, htmlElement, inheritedProperties, sourceContext);
                        break;

                    case "ol":
                    case "ul":
                    case "dir": //  treat as UL element
                    case "menu": //  treat as UL element
                        // List element conversion
                        AddList(xamlParentElement, htmlElement, inheritedProperties, sourceContext);
                        break;
                    case "li":
                        // LI outside of OL/UL
                        // Collect all sibling LIs, wrap them into a List and then proceed with the element following the last of LIs
                        htmlNode = AddOrphanListItems(xamlParentElement, htmlElement, inheritedProperties, sourceContext);
                        break;

                    case "img":
                        // TODO: Add image processing
                        AddImage(xamlParentElement, htmlElement, inheritedProperties, sourceContext);
                        break;

                    case "table":
                        // hand off to table parsing function which will perform special table syntax checks
                        AddTable(xamlParentElement, htmlElement, inheritedProperties, sourceContext);
                        break;

                    case "tbody":
                    case "tfoot":
                    case "thead":
                    case "tr":
                    case "td":
                    case "th":
                        // Table stuff without table wrapper
                        // TODO: add special-case processing here for elements that should be within tables when the
                        // parent element is NOT a table. If the parent element is a table they can be processed normally.
                        // we need to compare against the parent element here, we can't just break on a switch
                        goto default; // Thus we will skip this element as unknown, but still recurse into it.

                    case "style": // We already pre-processed all style elements. Ignore it now
                    case "meta":
                    case "head":
                    case "title":
                    case "script":
                        // Ignore these elements
                        break;

                    default:
                        // Wrap a sequence of inlines into an implicit paragraph
                        htmlNode = AddImplicitParagraph(xamlParentElement, htmlElement, inheritedProperties, sourceContext);
                        break;
                }

                // Remove the element from the stack
                Debug.Assert(sourceContext.Count > 0 && sourceContext[sourceContext.Count - 1] == htmlElement);
                sourceContext.RemoveAt(sourceContext.Count - 1);
            }

            // Return last processed node
            return htmlNode;
        }

        // .............................................................
        //
        // Line Breaks
        //
        // .............................................................

        private static void AddBreak(IXmlNode xamlParentElement, string htmlElementName)
        {
            // Create new xaml element corresponding to this html element
            XmlElement xamlLineBreak = xamlParentElement.OwnerDocument.CreateElementNS(_xamlNamespace, HtmlToXamlConverter.Xaml_LineBreak);

            xamlParentElement.AppendChild(xamlLineBreak);
            
            if (htmlElementName == "hr")
            {
                XmlText xamlHorizontalLine = xamlParentElement.OwnerDocument.CreateTextNode("----------------------");
            
                xamlParentElement.AppendChild(xamlHorizontalLine);

                xamlLineBreak = xamlParentElement.OwnerDocument.CreateElementNS(_xamlNamespace, HtmlToXamlConverter.Xaml_LineBreak);
                
                xamlParentElement.AppendChild(xamlLineBreak);
            }
        }

        // .............................................................
        //
        // Text Flow Elements
        //
        // .............................................................

        /// <summary>
        /// Generates Section or Paragraph element from DIV depending whether it contains any block elements or not
        /// </summary>
        /// <param name="xamlParentElement">
        /// XmlElement representing Xaml parent to which the converted element should be added
        /// </param>
        /// <param name="htmlElement">
        /// XmlElement representing Html element to be converted
        /// </param>
        /// <param name="inheritedProperties">
        /// properties inherited from parent context
        /// </param>
        /// 
        /// <param name="sourceContext"></param>
        /// true indicates that a content added by this call contains at least one block element
        /// </param>
        private static void AddSection(IXmlNode xamlParentElement, IXmlNode htmlElement, Dictionary<string, string> inheritedProperties, List<IXmlNode> sourceContext)
        {
            // Analyze the content of htmlElement to decide what xaml element to choose - Section or Paragraph.
            // If this Div has at least one block child then we need to use Section, otherwise use Paragraph
            bool htmlElementContainsBlocks = false;

            for (var htmlChildNode = htmlElement.FirstChild; htmlChildNode != null; htmlChildNode = htmlChildNode.NextSibling)
            {
                if (htmlChildNode is XmlElement)
                {
                    string htmlChildName = ((string)htmlChildNode.LocalName).ToLower();

                    if (HtmlSchema.IsBlockElement(htmlChildName))
                    {
                        htmlElementContainsBlocks = true;

                        break;
                    }
                }
            }

            if (!htmlElementContainsBlocks)
            {
                // The Div does not contain any block elements, so we can treat it as a Paragraph
                AddParagraph(xamlParentElement, htmlElement, inheritedProperties, sourceContext);
            }
            else
            {
                // The Div has some nested blocks, so we treat it as a Section

                // Create currentProperties as a compilation of local and inheritedProperties, set localProperties
                Dictionary<string,string> localProperties;
                Dictionary<string, string> currentProperties = GetElementProperties(htmlElement, inheritedProperties, out localProperties, sourceContext);

                // Create a XAML element corresponding to this html element
                XmlElement xamlElement = xamlParentElement.OwnerDocument.CreateElementNS(_xamlNamespace, HtmlToXamlConverter.Xaml_Section);
                
                ApplyLocalProperties(xamlElement, localProperties, /*isBlock:*/true);

                // Decide whether we can unwrap this element as not having any formatting significance.
                if (xamlElement.Attributes.Count == 0)
                {
                    // This elements is a group of block elements whitout any additional formatting.
                    // We can add blocks directly to xamlParentElement and avoid
                    // creating unnecessary Sections nesting.
                    xamlElement = xamlParentElement as XmlElement;
                }

                // Recurse into element subtree
                for (var htmlChildNode = htmlElement.FirstChild; htmlChildNode != null; htmlChildNode = htmlChildNode != null ? htmlChildNode.NextSibling : null)
                {
                    htmlChildNode = AddBlock(xamlElement, htmlChildNode, currentProperties, sourceContext);
                }

                // Add the new element to the parent.
                if (xamlElement != xamlParentElement)
                {
                    xamlParentElement.AppendChild(xamlElement);
                }
            }
        }

        /// <summary>
        /// Generates Paragraph element from P, H1-H7, Center etc.
        /// </summary>
        /// <param name="xamlParentElement">
        /// XmlElement representing Xaml parent to which the converted element should be added
        /// </param>
        /// <param name="htmlElement">
        /// XmlElement representing Html element to be converted
        /// </param>
        /// <param name="inheritedProperties">
        /// properties inherited from parent context
        /// </param>
        /// 
        /// <param name="sourceContext"></param>
        /// true indicates that a content added by this call contains at least one block element
        /// </param>
        private static void AddParagraph(IXmlNode xamlParentElement, IXmlNode htmlElement, Dictionary<string, string> inheritedProperties, List<IXmlNode> sourceContext)
        {
            // Create currentProperties as a compilation of local and inheritedProperties, set localProperties
            Dictionary<string,string> localProperties;
            Dictionary<string, string> currentProperties = GetElementProperties(htmlElement, inheritedProperties, out localProperties, sourceContext);

            // Create a XAML element corresponding to this html element
            XmlElement xamlElement = xamlParentElement.OwnerDocument.CreateElementNS(_xamlNamespace, HtmlToXamlConverter.Xaml_Paragraph);
            
            ApplyLocalProperties(xamlElement, localProperties, /*isBlock:*/true);

            // Recurse into element subtree
            for (var htmlChildNode = htmlElement.FirstChild; htmlChildNode != null; htmlChildNode = htmlChildNode.NextSibling)
            {
                AddInline(xamlElement, htmlChildNode, currentProperties, sourceContext);
            }

            // Add the new element to the parent.
            xamlParentElement.AppendChild(xamlElement);
        }

        /// <summary>
        /// Creates a Paragraph element and adds all nodes starting from htmlNode
        /// converted to appropriate Inlines.
        /// </summary>
        /// <param name="xamlParentElement">
        /// XmlElement representing Xaml parent to which the converted element should be added
        /// </param>
        /// <param name="htmlNode">
        /// XmlElement starting a collection of implicitly wrapped inlines.
        /// </param>
        /// <param name="inheritedProperties">
        /// properties inherited from parent context
        /// </param>
        /// 
        /// <param name="sourceContext"></param>
        /// true indicates that a content added by this call contains at least one block element
        /// </param>
        /// <returns>
        /// The last htmlNode added to the implicit paragraph
        /// </returns>
        private static IXmlNode AddImplicitParagraph(IXmlNode xamlParentElement, IXmlNode htmlNode, Dictionary<string, string> inheritedProperties, List<IXmlNode> sourceContext)
        {
            // Collect all non-block elements and wrap them into implicit Paragraph
            var xamlParagraph = xamlParentElement.OwnerDocument.CreateElementNS(_xamlNamespace, HtmlToXamlConverter.Xaml_Paragraph);

            IXmlNode lastNodeProcessed = null;

            while (htmlNode != null)
            {
                if (htmlNode.NodeType == NodeType.CommentNode)
                {
                    DefineInlineFragmentParent(htmlNode, null);
                }
                else if (htmlNode.NodeType == NodeType.TextNode)
                {
                    if (((string)htmlNode.NodeValue).Trim().Length > 0)
                    {
                        AddTextRun(xamlParagraph, (string)htmlNode.NodeValue);
                    }
                }
                else if (htmlNode is XmlElement)
                {
                    string htmlChildName = ((string)htmlNode.LocalName).ToLower();

                    if (HtmlSchema.IsBlockElement(htmlChildName))
                    {
                        // The sequence of non-blocked inlines ended. Stop implicit loop here.
                        break;
                    }
                    else
                    {
                        AddInline(xamlParagraph, htmlNode, inheritedProperties, sourceContext);
                    }
                }

                // Store last processed node to return it at the end
                lastNodeProcessed = htmlNode;

                htmlNode = htmlNode.NextSibling;
            }

            // Add the Paragraph to the parent
            // If only whitespaces and commens have been encountered,
            // then we have nothing to add in implicit paragraph; forget it.
            if (xamlParagraph.FirstChild != null)
            {
                xamlParentElement.AppendChild(xamlParagraph);
            }

            // Need to return last processed node
            return lastNodeProcessed;
        }

        // .............................................................
        //
        // Inline Elements
        //
        // .............................................................

        private static void AddInline(IXmlNode xamlParentElement, IXmlNode htmlNode, Dictionary<string, string> inheritedProperties, List<IXmlNode> sourceContext)
        {
            if (htmlNode.NodeType == NodeType.CommentNode)
            {
                DefineInlineFragmentParent(htmlNode, xamlParentElement);
            }
            else if (htmlNode.NodeType == NodeType.TextNode)
            {
                AddTextRun(xamlParentElement, (string)htmlNode.NodeValue);
            }
            else if (htmlNode.NodeType == NodeType.ElementNode)
            {
                var htmlElement = htmlNode as XmlElement;

                // Check whether this is an html element
                if ((string)htmlElement.NamespaceUri != HtmlParser.XhtmlNamespace)
                {
                    return; // Skip non-html elements
                }

                // Identify element name
                string htmlElementName = htmlElement.LocalName.ToLower();

                // Put source element to the stack
                sourceContext.Add(htmlElement);

                switch (htmlElementName)
                {
                    case "a":
                        AddHyperlink(xamlParentElement, htmlElement, inheritedProperties, sourceContext);
                        break;
                    case "img":
                        AddImage(xamlParentElement, htmlElement, inheritedProperties, sourceContext);
                        break;
                    case "br":
                    case "hr":
                        AddBreak(xamlParentElement, htmlElementName);
                        break;
                    default:
                        if (HtmlSchema.IsInlineElement(htmlElementName) || HtmlSchema.IsBlockElement(htmlElementName))
                        {
                            // Note: actually we do not expect block elements here,
                            // but if it happens to be here, we will treat it as a Span.

                            AddSpanOrRun(xamlParentElement, htmlElement, inheritedProperties, sourceContext);
                        }
                        break;
                }
                // Ignore all other elements non-(block/inline/image)

                // Remove the element from the stack
                Debug.Assert(sourceContext.Count > 0 && sourceContext[sourceContext.Count - 1] == htmlElement);
                sourceContext.RemoveAt(sourceContext.Count - 1);
            }
        }

        private static void AddSpanOrRun(IXmlNode xamlParentElement, IXmlNode htmlElement, Dictionary<string, string> inheritedProperties, List<IXmlNode> sourceContext)
        {
            // Decide what XAML element to use for this inline element.
            // Check whether it contains any nested inlines
            bool elementHasChildren = false;

            for (var htmlNode = htmlElement.FirstChild; htmlNode != null; htmlNode = htmlNode.NextSibling)
            {
                if (htmlNode is XmlElement)
                {
                    string htmlChildName = ((string)htmlNode.LocalName).ToLower();

                    if (HtmlSchema.IsInlineElement(htmlChildName) || 
                        HtmlSchema.IsBlockElement(htmlChildName) || 
                        htmlChildName == "img" || 
                        htmlChildName == "br" || 
                        htmlChildName == "hr")
                    {
                        elementHasChildren = true;
                    
                        break;
                    }
                }
            }

            string xamlElementName = elementHasChildren ? HtmlToXamlConverter.Xaml_Span : HtmlToXamlConverter.Xaml_Run;

            // Create currentProperties as a compilation of local and inheritedProperties, set localProperties
            Dictionary<string,string> localProperties;
            Dictionary<string, string> currentProperties = GetElementProperties(htmlElement, inheritedProperties, out localProperties, sourceContext);

            // Create a XAML element corresponding to this html element
            XmlElement xamlElement = xamlParentElement.OwnerDocument.CreateElementNS(_xamlNamespace, xamlElementName);
            
            ApplyLocalProperties(xamlElement, localProperties, /*isBlock:*/false);

            // Recurse into element subtree
            for (var htmlChildNode = htmlElement.FirstChild; htmlChildNode != null; htmlChildNode = htmlChildNode.NextSibling)
            {
                AddInline(xamlElement, htmlChildNode, currentProperties, sourceContext);
            }

            // Add the new element to the parent.
            xamlParentElement.AppendChild(xamlElement);
        }

        // Adds a text run to a xaml tree
        private static void AddTextRun(IXmlNode xamlElement, string textData)
        {
            // Remove control characters
            for (int i = 0; i < textData.Length; i++)
            {
                if (Char.IsControl(textData[i]))
                {
                    textData = textData.Remove(i--, 1);  // decrement i to compensate for character removal
                }
            }

            // Replace No-Breaks by spaces (160 is a code of &nbsp; entity in html)
            //  This is a work around since WPF/XAML does not support &nbsp.
            textData = textData.Replace((char)160, ' ');

            if (textData.Length > 0)
            {
                xamlElement.AppendChild(xamlElement.OwnerDocument.CreateTextNode(textData));
            }
        }

        private static void AddHyperlink(IXmlNode xamlParentElement, IXmlNode htmlElement, Dictionary<string, string> inheritedProperties, List<IXmlNode> sourceContext)
        {
            // Convert href attribute into NavigateUri and TargetName
            string href = GetAttribute(htmlElement, "href");
            if (href == null)
            {
                // When href attribute is missing - ignore the hyperlink
                AddSpanOrRun(xamlParentElement, htmlElement, inheritedProperties, sourceContext);
            }
            else
            {
                // Create currentProperties as a compilation of local and inheritedProperties, set localProperties
                Dictionary<string,string> localProperties;
                Dictionary<string, string> currentProperties = GetElementProperties(htmlElement, inheritedProperties, out localProperties, sourceContext);

                // Create a XAML element corresponding to this html element
                XmlElement xamlElement = xamlParentElement.OwnerDocument.CreateElementNS(_xamlNamespace, HtmlToXamlConverter.Xaml_Hyperlink);
                
                ApplyLocalProperties(xamlElement, localProperties, /*isBlock:*/false);

                string[] hrefParts = href.Split(new char[] { '#' });
                if (hrefParts.Length > 0 && hrefParts[0].Trim().Length > 0)
                {
                    xamlElement.SetAttribute(HtmlToXamlConverter.Xaml_Hyperlink_NavigateUri, hrefParts[0].Trim());
                }
                if (hrefParts.Length == 2 && hrefParts[1].Trim().Length > 0)
                {
                    xamlElement.SetAttribute(HtmlToXamlConverter.Xaml_Hyperlink_TargetName, hrefParts[1].Trim());
                }

                // Recurse into element subtree
                for (var htmlChildNode = htmlElement.FirstChild; htmlChildNode != null; htmlChildNode = htmlChildNode.NextSibling)
                {
                    AddInline(xamlElement, htmlChildNode, currentProperties, sourceContext);
                }

                // Add the new element to the parent.
                xamlParentElement.AppendChild(xamlElement);
            }
        }

        // Stores a parent xaml element for the case when selected fragment is inline.
        private static IXmlNode InlineFragmentParentElement;

        // Called when html comment is encountered to store a parent element
        // for the case when the fragment is inline - to extract it to a separate
        // Span wrapper after the conversion.
        private static void DefineInlineFragmentParent(IXmlNode htmlComment, IXmlNode xamlParentElement)
        {
            if ((string)htmlComment.NodeValue == "StartFragment")
            {
                InlineFragmentParentElement = xamlParentElement;
            }
            else if ((string)htmlComment.NodeValue == "EndFragment")
            {
                if (InlineFragmentParentElement == null && xamlParentElement != null)
                {
                    // Normally this cannot happen if comments produced by correct copying code
                    // in Word or IE, but when it is produced manually then fragment boundary
                    // markers can be inconsistent. In this case StartFragment takes precedence,
                    // but if it is not set, then we get the value from EndFragment marker.
                    InlineFragmentParentElement = xamlParentElement;
                }
            }
        }

        // Extracts a content of an element stored as InlineFragmentParentElement
        // into a separate Span wrapper.
        // Note: when selected content does not cross paragraph boundaries,
        // the fragment is marked within
        private static IXmlNode ExtractInlineFragment(IXmlNode xamlFlowDocumentElement)
        {
            if (InlineFragmentParentElement != null)
            {
                if ((string)InlineFragmentParentElement.LocalName == HtmlToXamlConverter.Xaml_Span)
                {
                    xamlFlowDocumentElement = InlineFragmentParentElement;
                }
                else
                {
                    xamlFlowDocumentElement = xamlFlowDocumentElement.OwnerDocument.CreateElementNS(_xamlNamespace, HtmlToXamlConverter.Xaml_Span);
                    while (InlineFragmentParentElement.FirstChild != null)
                    {
                        var copyNode = InlineFragmentParentElement.FirstChild;

                        InlineFragmentParentElement.RemoveChild(copyNode);
                        
                        xamlFlowDocumentElement.AppendChild(copyNode);
                    }
                }
            }

            return xamlFlowDocumentElement;
        }

        // .............................................................
        //
        // Images
        //
        // .............................................................

        private static void AddImage(IXmlNode xamlParentElement, IXmlNode htmlElement, Dictionary<string, string> inheritedProperties, List<IXmlNode> sourceContext)
        {
            //  Implement images
        }

        // .............................................................
        //
        // Lists
        //
        // .............................................................

        /// <summary>
        /// Converts Html ul or ol element into Xaml list element. During conversion if the ul/ol element has any children 
        /// that are not li elements, they are ignored and not added to the list element
        /// </summary>
        /// <param name="xamlParentElement">
        /// XmlElement representing Xaml parent to which the converted element should be added
        /// </param>
        /// <param name="htmlListElement">
        /// XmlElement representing Html ul/ol element to be converted
        /// </param>
        /// <param name="inheritedProperties">
        /// properties inherited from parent context
        /// </param>
        /// 
        /// <param name="sourceContext"></param>
        private static void AddList(IXmlNode xamlParentElement, IXmlNode htmlListElement, Dictionary<string, string> inheritedProperties, List<IXmlNode> sourceContext)
        {
            string htmlListElementName = htmlListElement.LocalName.ToLower();

            Dictionary<string,string> localProperties;
            Dictionary<string, string> currentProperties = GetElementProperties(htmlListElement, inheritedProperties, out localProperties, sourceContext);

            // Create Xaml List element
            XmlElement xamlListElement = xamlParentElement.OwnerDocument.CreateElementNS(_xamlNamespace, Xaml_List);

            // Set default list markers
            if (htmlListElementName == "ol")
            {
                // Ordered list
                xamlListElement.SetAttribute(HtmlToXamlConverter.Xaml_List_MarkerStyle, Xaml_List_MarkerStyle_Decimal);
            }
            else
            {
                // Unordered list - all elements other than OL treated as unordered lists
                xamlListElement.SetAttribute(HtmlToXamlConverter.Xaml_List_MarkerStyle, Xaml_List_MarkerStyle_Disc);
            }

            // Apply local properties to list to set marker attribute if specified
            // TODO: Should we have separate list attribute processing function?
            ApplyLocalProperties(xamlListElement, localProperties, /*isBlock:*/true);

            // Recurse into list subtree
            for (var htmlChildNode = htmlListElement.FirstChild; htmlChildNode != null; htmlChildNode = htmlChildNode.NextSibling)
            {
                if (htmlChildNode.NodeType == NodeType.ElementNode && htmlChildNode.LocalName.ToLower() == "li")
                {
                    sourceContext.Add(htmlChildNode);

                    AddListItem(xamlListElement, htmlChildNode, currentProperties, sourceContext);
                    
                    Debug.Assert(sourceContext.Count > 0 && sourceContext[sourceContext.Count - 1] == htmlChildNode);
                    
                    sourceContext.RemoveAt(sourceContext.Count - 1);
                }
                else
                {
                    // Not an li element. Add it to previous ListBoxItem
                    //  We need to append the content to the end
                    // of a previous list item.
                }
            }

            // Add the List element to xaml tree - if it is not empty
            if (xamlListElement.HasChildNodes())
            {
                xamlParentElement.AppendChild(xamlListElement);
            }
        }

        /// <summary>
        /// If li items are found without a parent ul/ol element in Html string, creates xamlListElement as their parent and adds
        /// them to it. If the previously added node to the same xamlParentElement was a List, adds the elements to that list.
        /// Otherwise, we create a new xamlListElement and add them to it. Elements are added as long as li elements appear sequentially.
        /// The first non-li or text node stops the addition.
        /// </summary>
        /// <param name="xamlParentElement">
        /// Parent element for the list
        /// </param>
        /// <param name="htmlLIElement">
        /// Start Html li element without parent list
        /// </param>
        /// <param name="inheritedProperties">
        /// Properties inherited from parent context
        /// </param>
        /// <returns>
        /// XmlElement representing the first non-li node in the input after one or more li's have been processed.
        /// </returns>
        private static IXmlNode AddOrphanListItems(IXmlNode xamlParentElement, IXmlNode htmlLIElement, Dictionary<string, string> inheritedProperties, List<IXmlNode> sourceContext)
        {
            Debug.Assert(htmlLIElement.LocalName.ToLower() == "li");

            IXmlNode lastProcessedListItemElement = null;

            // Find out the last element attached to the xamlParentElement, which is the previous sibling of this node
            var xamlListItemElementPreviousSibling = xamlParentElement.LastChild;
            
            IXmlNode xamlListElement;

            if (xamlListItemElementPreviousSibling != null && (string)xamlListItemElementPreviousSibling.LocalName == Xaml_List)
            {
                // Previously added Xaml element was a list. We will add the new li to it
                xamlListElement = xamlListItemElementPreviousSibling;
            }
            else
            {
                // No list element near. Create our own.
                xamlListElement = xamlParentElement.OwnerDocument.CreateElementNS(_xamlNamespace, Xaml_List);
                
                xamlParentElement.AppendChild(xamlListElement);
            }

            var htmlChildNode = htmlLIElement;

            string htmlChildNodeName = htmlChildNode == null ? null : htmlChildNode.LocalName.ToLower();

            //  Current element properties missed here.
            //currentProperties = GetElementProperties(htmlLIElement, inheritedProperties, out localProperties, stylesheet);

            // Add li elements to the parent xamlListElement we created as long as they appear sequentially
            // Use properties inherited from xamlParentElement for context 
            while (htmlChildNode != null && htmlChildNodeName == "li")
            {
                AddListItem(xamlListElement, htmlChildNode, inheritedProperties, sourceContext);

                lastProcessedListItemElement = htmlChildNode;
                
                htmlChildNode = htmlChildNode.NextSibling;               
                htmlChildNodeName = htmlChildNode == null ? null : htmlChildNode.LocalName.ToLower();
            }

            return lastProcessedListItemElement;
        }

        /// <summary>
        /// Converts htmlLIElement into Xaml ListItem element, and appends it to the parent xamlListElement
        /// </summary>
        /// <param name="xamlListElement">
        /// XmlElement representing Xaml List element to which the converted td/th should be added
        /// </param>
        /// <param name="htmlLIElement">
        /// XmlElement representing Html li element to be converted
        /// </param>
        /// <param name="inheritedProperties">
        /// Properties inherited from parent context
        /// </param>
        private static void AddListItem(IXmlNode xamlListElement, IXmlNode htmlLIElement, Dictionary<string, string> inheritedProperties, List<IXmlNode> sourceContext)
        {
            // Parameter validation
            Debug.Assert(xamlListElement != null);
            Debug.Assert((string)xamlListElement.LocalName == Xaml_List);
            Debug.Assert(htmlLIElement != null);
            Debug.Assert(htmlLIElement.LocalName.ToLower() == "li");
            Debug.Assert(inheritedProperties != null);

            Dictionary<string,string> localProperties;
            Dictionary<string, string> currentProperties = GetElementProperties(htmlLIElement, inheritedProperties, out localProperties, sourceContext);

            XmlElement xamlListItemElement = xamlListElement.OwnerDocument.CreateElementNS(_xamlNamespace, Xaml_ListItem);

            // TODO: process local properties for li element

            // Process children of the ListItem
            for (var htmlChildNode = htmlLIElement.FirstChild; htmlChildNode != null; htmlChildNode = htmlChildNode != null ? htmlChildNode.NextSibling : null)
            {
                htmlChildNode = AddBlock(xamlListItemElement, htmlChildNode, currentProperties, sourceContext);
            }

            // Add resulting ListBoxItem to a xaml parent
            xamlListElement.AppendChild(xamlListItemElement);
        }

        // .............................................................
        //
        // Tables
        //
        // .............................................................

        /// <summary>
        /// Converts htmlTableElement to a Xaml Table element. Adds tbody elements if they are missing so
        /// that a resulting Xaml Table element is properly formed.
        /// </summary>
        /// <param name="xamlParentElement">
        /// Parent xaml element to which a converted table must be added.
        /// </param>
        /// <param name="htmlTableElement">
        /// XmlElement reprsenting the Html table element to be converted
        /// </param>
        /// <param name="inheritedProperties">
        /// Dictionary<string,string> representing properties inherited from parent context. 
        /// </param>
        private static void AddTable(IXmlNode xamlParentElement, IXmlNode htmlTableElement, Dictionary<string, string> inheritedProperties, List<IXmlNode> sourceContext)
        {
            // Parameter validation
            Debug.Assert(htmlTableElement.LocalName.ToLower() == "table");
            Debug.Assert(xamlParentElement != null);
            Debug.Assert(inheritedProperties != null);

            // Create current properties to be used by children as inherited properties, set local properties
            Dictionary<string,string> localProperties;
            Dictionary<string, string> currentProperties = GetElementProperties(htmlTableElement, inheritedProperties, out localProperties, sourceContext);

            // TODO: process localProperties for tables to override defaults, decide cell spacing defaults

            // Check if the table contains only one cell - we want to take only its content
            var singleCell = GetCellFromSingleCellTable(htmlTableElement);

            if (singleCell != null)
            {
                //  Need to push skipped table elements onto sourceContext
                sourceContext.Add(singleCell);

                // Add the cell's content directly to parent
                for (var htmlChildNode = singleCell.FirstChild; htmlChildNode != null; htmlChildNode = htmlChildNode != null ? htmlChildNode.NextSibling : null)
                {
                    htmlChildNode = AddBlock(xamlParentElement, htmlChildNode, currentProperties, sourceContext);
                }

                Debug.Assert(sourceContext.Count > 0 && sourceContext[sourceContext.Count - 1] == singleCell);
                sourceContext.RemoveAt(sourceContext.Count - 1);
            }
            else
            {
                // Create xamlTableElement
                XmlElement xamlTableElement = xamlParentElement.OwnerDocument.CreateElementNS(_xamlNamespace, Xaml_Table);

                // Analyze table structure for column widths and rowspan attributes
                List<double> columnStarts = AnalyzeTableStructure(htmlTableElement);

                // Process COLGROUP & COL elements
                AddColumnInformation(htmlTableElement, xamlTableElement, columnStarts, currentProperties, sourceContext);

                // Process table body - TBODY and TR elements
                var htmlChildNode = htmlTableElement.FirstChild;

                while (htmlChildNode != null)
                {
                    string htmlChildName = htmlChildNode.LocalName.ToLower();

                    // Process the element
                    if (htmlChildName == "tbody" || htmlChildName == "thead" || htmlChildName == "tfoot")
                    {
                        //  Add more special processing for TableHeader and TableFooter
                        XmlElement xamlTableBodyElement = xamlTableElement.OwnerDocument.CreateElementNS(_xamlNamespace, Xaml_TableRowGroup);

                        xamlTableElement.AppendChild(xamlTableBodyElement);

                        sourceContext.Add(htmlChildNode);

                        // Get properties of Html tbody element
                        Dictionary<string,string> tbodyElementLocalProperties;
                        Dictionary<string, string> tbodyElementCurrentProperties = GetElementProperties(htmlChildNode, currentProperties, out tbodyElementLocalProperties, sourceContext);
                        // TODO: apply local properties for tbody

                        // Process children of htmlChildNode, which is tbody, for tr elements
                        AddTableRowsToTableBody(xamlTableBodyElement, htmlChildNode.FirstChild, tbodyElementCurrentProperties, columnStarts, sourceContext);

                        if (xamlTableBodyElement.HasChildNodes())
                        {
                            xamlTableElement.AppendChild(xamlTableBodyElement);
                            // else: if there is no TRs in this TBody, we simply ignore it
                        }

                        Debug.Assert(sourceContext.Count > 0 && sourceContext[sourceContext.Count - 1] == htmlChildNode);
                        sourceContext.RemoveAt(sourceContext.Count - 1);

                        htmlChildNode = htmlChildNode.NextSibling;
                    }
                    else if (htmlChildName == "tr")
                    {
                        // Tbody is not present, but tr element is present. Tr is wrapped in tbody
                        XmlElement xamlTableBodyElement = xamlTableElement.OwnerDocument.CreateElementNS(_xamlNamespace, Xaml_TableRowGroup);

                        // We use currentProperties of xamlTableElement when adding rows since the tbody element is artificially created and has 
                        // no properties of its own

                        htmlChildNode = AddTableRowsToTableBody(xamlTableBodyElement, htmlChildNode, currentProperties, columnStarts, sourceContext);
                        
                        if (xamlTableBodyElement.HasChildNodes())
                        {
                            xamlTableElement.AppendChild(xamlTableBodyElement);
                        }
                    }
                    else
                    {
                        // Element is not tbody or tr. Ignore it.
                        // TODO: add processing for thead, tfoot elements and recovery for td elements
                        htmlChildNode = htmlChildNode.NextSibling;
                    }
                }

                if (xamlTableElement.HasChildNodes())
                {
                    xamlParentElement.AppendChild(xamlTableElement);
                }
            }
        }

        private static IXmlNode GetCellFromSingleCellTable(IXmlNode htmlTableElement)
        {
            IXmlNode singleCell = null;

            for (var tableChild = htmlTableElement.FirstChild; tableChild != null; tableChild = tableChild.NextSibling)
            {
                string elementName = tableChild.LocalName.ToLower();
                if (elementName == "tbody" || elementName == "thead" || elementName == "tfoot")
                {
                    if (singleCell != null)
                    {
                        return null;
                    }
                    for (var tbodyChild = tableChild.FirstChild; tbodyChild != null; tbodyChild = tbodyChild.NextSibling)
                    {
                        if (tbodyChild.LocalName.ToLower() == "tr")
                        {
                            if (singleCell != null)
                            {
                                return null;
                            }
                            for (var trChild = tbodyChild.FirstChild; trChild != null; trChild = trChild.NextSibling)
                            {
                                string cellName = trChild.LocalName.ToLower();

                                if (cellName == "td" || cellName == "th")
                                {
                                    if (singleCell != null)
                                    {
                                        return null;
                                    }

                                    singleCell = trChild;
                                }
                            }
                        }
                    }
                }
                else if (tableChild.LocalName.ToLower() == "tr")
                {
                    if (singleCell != null)
                    {
                        return null;
                    }
                    for (var trChild = tableChild.FirstChild; trChild != null; trChild = trChild.NextSibling)
                    {
                        string cellName = trChild.LocalName.ToLower();

                        if (cellName == "td" || cellName == "th")
                        {
                            if (singleCell != null)
                            {
                                return null;
                            }

                            singleCell = trChild;
                        }
                    }
                }
            }

            return singleCell;
        }

        /// <summary>
        /// Processes the information about table columns - COLGROUP and COL html elements.
        /// </summary>
        /// <param name="htmlTableElement">
        /// XmlElement representing a source html table.
        /// </param>
        /// <param name="xamlTableElement">
        /// XmlElement repesenting a resulting xaml table.
        /// </param>
        /// <param name="columnStartsAllRows">
        /// Array of doubles - column start coordinates.
        /// Can be null, which means that column size information is not available
        /// and we must use source colgroup/col information.
        /// In case wneh it's not null, we will ignore source colgroup/col information.
        /// </param>
        /// <param name="currentProperties"></param>
        /// 
        /// <param name="sourceContext"></param>
        private static void AddColumnInformation(IXmlNode htmlTableElement, IXmlNode xamlTableElement, List<double> columnStartsAllRows, Dictionary<string, string> currentProperties, List<IXmlNode> sourceContext)
        {
            // Add column information
            if (columnStartsAllRows != null)
            {
                // We have consistent information derived from table cells; use it
                // The last element in columnStarts represents the end of the table
                for (int columnIndex = 0; columnIndex < columnStartsAllRows.Count - 1; columnIndex++)
                {
                    XmlElement xamlColumnElement;

                    xamlColumnElement = xamlTableElement.OwnerDocument.CreateElementNS(_xamlNamespace, Xaml_TableColumn);

                    xamlColumnElement.SetAttribute(Xaml_Width, ((double)columnStartsAllRows[columnIndex + 1] - (double)columnStartsAllRows[columnIndex]).ToString());
                    
                    xamlTableElement.AppendChild(xamlColumnElement);
                }
            }
            else
            {
                // We do not have consistent information from table cells;
                // Translate blindly colgroups from html.                
                for (var htmlChildNode = htmlTableElement.FirstChild; htmlChildNode != null; htmlChildNode = htmlChildNode.NextSibling)
                {
                    if (htmlChildNode.LocalName.ToLower() == "colgroup")
                    {
                        // TODO: add column width information to this function as a parameter and process it
                        AddTableColumnGroup(xamlTableElement, htmlChildNode, currentProperties, sourceContext);
                    }
                    else if (htmlChildNode.LocalName.ToLower() == "col")
                    {
                        AddTableColumn(xamlTableElement, htmlChildNode, currentProperties, sourceContext);
                    }
                    else if (htmlChildNode is XmlElement)
                    {
                        // Some element which belongs to table body. Stop column loop.
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Converts htmlColgroupElement into Xaml TableColumnGroup element, and appends it to the parent
        /// xamlTableElement
        /// </summary>
        /// <param name="xamlTableElement">
        /// XmlElement representing Xaml Table element to which the converted column group should be added
        /// </param>
        /// <param name="htmlColgroupElement">
        /// XmlElement representing Html colgroup element to be converted
        /// <param name="inheritedProperties">
        /// Properties inherited from parent context
        /// </param>
        private static void AddTableColumnGroup(IXmlNode xamlTableElement, IXmlNode htmlColgroupElement, Dictionary<string, string> inheritedProperties, List<IXmlNode> sourceContext)
        {
            Dictionary<string,string> localProperties;
            Dictionary<string, string> currentProperties = GetElementProperties(htmlColgroupElement, inheritedProperties, out localProperties, sourceContext);

            // TODO: process local properties for colgroup

            // Process children of colgroup. Colgroup may contain only col elements.
            for (var htmlNode = htmlColgroupElement.FirstChild; htmlNode != null; htmlNode = htmlNode.NextSibling)
            {
                if (htmlNode is XmlElement && ((string)htmlNode.LocalName).ToLower() == "col")
                {
                    AddTableColumn(xamlTableElement, htmlNode, currentProperties, sourceContext);
                }
            }
        }

        /// <summary>
        /// Converts htmlColElement into Xaml TableColumn element, and appends it to the parent
        /// xamlTableColumnGroupElement
        /// </summary>
        /// <param name="xamlTableElement"></param>
        /// <param name="htmlColElement">
        /// XmlElement representing Html col element to be converted
        /// </param>
        /// <param name="inheritedProperties">
        /// properties inherited from parent context
        /// </param>
        /// 
        /// <param name="sourceContext"></param>
        private static void AddTableColumn(IXmlNode xamlTableElement, IXmlNode htmlColElement, Dictionary<string, string> inheritedProperties, List<IXmlNode> sourceContext)
        {
            Dictionary<string,string> localProperties;
            Dictionary<string, string> currentProperties = GetElementProperties(htmlColElement, inheritedProperties, out localProperties, sourceContext);

            XmlElement xamlTableColumnElement = xamlTableElement.OwnerDocument.CreateElementNS(_xamlNamespace, Xaml_TableColumn);

            // TODO: process local properties for TableColumn element

            // Col is an empty element, with no subtree 
            xamlTableElement.AppendChild(xamlTableColumnElement);
        }

        /// <summary>
        /// Adds TableRow elements to xamlTableBodyElement. The rows are converted from Html tr elements that
        /// may be the children of an Html tbody element or an Html table element with tbody missing
        /// </summary>
        /// <param name="xamlTableBodyElement">
        /// XmlElement representing Xaml TableRowGroup element to which the converted rows should be added
        /// </param>
        /// <param name="htmlTRStartNode">
        /// XmlElement representing the first tr child of the tbody element to be read
        /// </param>
        /// <param name="currentProperties">
        /// Dictionary<string,string> representing current properties of the tbody element that are generated and applied in the
        /// AddTable function; to be used as inheritedProperties when adding tr elements
        /// </param>
        /// <param name="columnStarts"></param>
        /// 
        /// <param name="sourceContext"></param>
        /// <returns>
        /// XmlElement representing the current position of the iterator among tr elements
        /// </returns>
        private static IXmlNode AddTableRowsToTableBody(IXmlNode xamlTableBodyElement, IXmlNode htmlTRStartNode, Dictionary<string, string> currentProperties, List<double> columnStarts, List<IXmlNode> sourceContext)
        {
            // Parameter validation
            Debug.Assert((string)xamlTableBodyElement.LocalName == Xaml_TableRowGroup);

            Debug.Assert(currentProperties != null);

            // Initialize child node for iteratimg through children to the first tr element
            var htmlChildNode = htmlTRStartNode;

            List<int> activeRowSpans = null;
            
            if (columnStarts != null)
            {
                activeRowSpans = new List<int>();

                InitializeActiveRowSpans(activeRowSpans, columnStarts.Count);
            }

            while (htmlChildNode != null && ((string)htmlChildNode.LocalName).ToLower() != "tbody")
            {
                if (((string)htmlChildNode.LocalName).ToLower() == "tr")
                {
                    XmlElement xamlTableRowElement = xamlTableBodyElement.OwnerDocument.CreateElementNS(_xamlNamespace, Xaml_TableRow);

                    sourceContext.Add(htmlChildNode);

                    // Get tr element properties
                    Dictionary<string,string> trElementLocalProperties;
                    Dictionary<string, string> trElementCurrentProperties = GetElementProperties(htmlChildNode, currentProperties, out trElementLocalProperties, sourceContext);
                    // TODO: apply local properties to tr element

                    AddTableCellsToTableRow(xamlTableRowElement, htmlChildNode.FirstChild, trElementCurrentProperties, columnStarts, activeRowSpans, sourceContext);
                    
                    if (xamlTableRowElement.HasChildNodes())
                    {
                        xamlTableBodyElement.AppendChild(xamlTableRowElement);
                    }

                    Debug.Assert(sourceContext.Count > 0 && sourceContext[sourceContext.Count - 1] == htmlChildNode);
                    sourceContext.RemoveAt(sourceContext.Count - 1);

                    // Advance
                    htmlChildNode = htmlChildNode.NextSibling;
                    
                }
                else if (htmlChildNode.LocalName.ToLower() == "td")
                {
                    // Tr element is not present. We create one and add td elements to it
                    XmlElement xamlTableRowElement = xamlTableBodyElement.OwnerDocument.CreateElementNS(_xamlNamespace, Xaml_TableRow);
                    
                    // This is incorrect formatting and the column starts should not be set in this case
                    Debug.Assert(columnStarts == null);

                    htmlChildNode = AddTableCellsToTableRow(xamlTableRowElement, htmlChildNode, currentProperties, columnStarts, activeRowSpans, sourceContext);
                    
                    if (xamlTableRowElement.HasChildNodes())
                    {
                        xamlTableBodyElement.AppendChild(xamlTableRowElement);
                    }
                }
                else 
                {
                    // Not a tr or td  element. Ignore it.
                    // TODO: consider better recovery here
                    htmlChildNode = htmlChildNode.NextSibling;
                }
            }

            return htmlChildNode;
        }

        /// <summary>
        /// Adds TableCell elements to xamlTableRowElement.
        /// </summary>
        /// <param name="xamlTableRowElement">
        /// XmlElement representing Xaml TableRow element to which the converted cells should be added
        /// </param>
        /// <param name="htmlTDStartNode">
        /// XmlElement representing the child of tr or tbody element from which we should start adding td elements
        /// </param>
        /// <param name="currentProperties">
        /// properties of the current html tr element to which cells are to be added
        /// </param>
        /// <returns>
        /// XmlElement representing the current position of the iterator among the children of the parent Html tbody/tr element
        /// </returns>
        private static IXmlNode AddTableCellsToTableRow(IXmlNode xamlTableRowElement, IXmlNode htmlTDStartNode, Dictionary<string, string> currentProperties, List<double> columnStarts, List<int> activeRowSpans, List<IXmlNode> sourceContext)
        {
            // parameter validation
            Debug.Assert((string)xamlTableRowElement.LocalName == Xaml_TableRow);
            Debug.Assert(currentProperties != null);
            if (columnStarts != null)
            {
                Debug.Assert(activeRowSpans.Count == columnStarts.Count);
            }

            var htmlChildNode = htmlTDStartNode;

            double columnStart = 0;
            double columnWidth = 0;

            int columnIndex = 0;
            int columnSpan = 0;

            while (htmlChildNode != null && htmlChildNode.LocalName.ToLower() != "tr" && htmlChildNode.LocalName.ToLower() != "tbody" && htmlChildNode.LocalName.ToLower() != "thead" && htmlChildNode.LocalName.ToLower() != "tfoot")
            {
                if (htmlChildNode.LocalName.ToLower() == "td" || htmlChildNode.LocalName.ToLower() == "th")
                {
                    XmlElement xamlTableCellElement = xamlTableRowElement.OwnerDocument.CreateElementNS(_xamlNamespace, Xaml_TableCell);

                    sourceContext.Add(htmlChildNode);

                    Dictionary<string,string> tdElementLocalProperties;
                    Dictionary<string, string> tdElementCurrentProperties = GetElementProperties(htmlChildNode, currentProperties, out tdElementLocalProperties, sourceContext);

                    // TODO: determine if localProperties can be used instead of htmlChildNode in this call, and if they can,
                    // make necessary changes and use them instead.
                    ApplyPropertiesToTableCellElement(htmlChildNode, xamlTableCellElement);

                    if (columnStarts != null)
                    {
                        Debug.Assert(columnIndex < columnStarts.Count - 1);
                        while (columnIndex < activeRowSpans.Count && (int)activeRowSpans[columnIndex] > 0)
                        {
                            activeRowSpans[columnIndex] = (int)activeRowSpans[columnIndex] - 1;
                            Debug.Assert((int)activeRowSpans[columnIndex] >= 0);
                            columnIndex++;
                        }
                        Debug.Assert(columnIndex < columnStarts.Count - 1);
                        columnStart = (double)columnStarts[columnIndex];
                        columnWidth = GetColumnWidth(htmlChildNode);
                        columnSpan = CalculateColumnSpan(columnIndex, columnWidth, columnStarts);
                        int rowSpan = GetRowSpan(htmlChildNode);

                        // Column cannot have no span
                        Debug.Assert(columnSpan > 0);
                        Debug.Assert(columnIndex + columnSpan < columnStarts.Count);

                        xamlTableCellElement.SetAttribute(Xaml_TableCell_ColumnSpan, columnSpan.ToString());
                        
                        // Apply row span
                        for (int spannedColumnIndex = columnIndex; spannedColumnIndex < columnIndex + columnSpan; spannedColumnIndex++)
                        {
                            Debug.Assert(spannedColumnIndex < activeRowSpans.Count);
                            activeRowSpans[spannedColumnIndex] = (rowSpan - 1);
                            Debug.Assert((int)activeRowSpans[spannedColumnIndex] >= 0);
                        }

                        columnIndex = columnIndex + columnSpan;
                    }

                    AddDataToTableCell(xamlTableCellElement, htmlChildNode.FirstChild, tdElementCurrentProperties, sourceContext);
                    
                    if (xamlTableCellElement.HasChildNodes())
                    {
                        xamlTableRowElement.AppendChild(xamlTableCellElement);
                    }

                    Debug.Assert(sourceContext.Count > 0 && sourceContext[sourceContext.Count - 1] == htmlChildNode);

                    sourceContext.RemoveAt(sourceContext.Count - 1);

                    htmlChildNode = htmlChildNode.NextSibling;
                }
                else
                {
                    // Not td element. Ignore it.
                    // TODO: Consider better recovery
                    htmlChildNode = htmlChildNode.NextSibling;
                }
            }

            return htmlChildNode;
        }

        /// <summary>
        /// adds table cell data to xamlTableCellElement
        /// </summary>
        /// <param name="xamlTableCellElement">
        /// XmlElement representing Xaml TableCell element to which the converted data should be added
        /// </param>
        /// <param name="htmlDataStartNode">
        /// XmlElement representing the start element of data to be added to xamlTableCellElement
        /// </param>
        /// <param name="currentProperties">
        /// Current properties for the html td/th element corresponding to xamlTableCellElement
        /// </param>
        private static void AddDataToTableCell(IXmlNode xamlTableCellElement, IXmlNode htmlDataStartNode, Dictionary<string, string> currentProperties, List<IXmlNode> sourceContext)
        {
            // Parameter validation
            Debug.Assert((string)xamlTableCellElement.LocalName == Xaml_TableCell);
            Debug.Assert(currentProperties != null);

            for (var htmlChildNode = htmlDataStartNode; htmlChildNode != null; htmlChildNode = htmlChildNode != null ? htmlChildNode.NextSibling : null)
            {
                // Process a new html element and add it to the td element
                htmlChildNode = AddBlock(xamlTableCellElement, htmlChildNode, currentProperties, sourceContext);
            }
        }

        /// <summary>
        /// Performs a parsing pass over a table to read information about column width and rowspan attributes. This information
        /// is used to determine the starting point of each column. 
        /// </summary>
        /// <param name="htmlTableElement">
        /// XmlElement representing Html table whose structure is to be analyzed
        /// </param>
        /// <returns>
        /// ArrayList of type double which contains the function output. If analysis is successful, this ArrayList contains
        /// all the points which are the starting position of any column in the table, ordered from left to right.
        /// In case if analisys was impossible we return null.
        /// </returns>
        private static List<double> AnalyzeTableStructure(IXmlNode htmlTableElement)
        {
            // Parameter validation
            Debug.Assert(htmlTableElement.LocalName.ToLower() == "table");

            if (!htmlTableElement.HasChildNodes())
            {
                return null;
            }

            bool columnWidthsAvailable = true;

            var columnStarts = new List<double>();
            var activeRowSpans = new List<int>();

            Debug.Assert(columnStarts.Count == activeRowSpans.Count);

            var htmlChildNode = htmlTableElement.FirstChild;

            double tableWidth = 0;  // Keep track of table width which is the width of its widest row

            // Analyze tbody and tr elements
            while (htmlChildNode != null && columnWidthsAvailable)
            {
                Debug.Assert(columnStarts.Count == activeRowSpans.Count);

                switch (htmlChildNode.LocalName.ToLower())
                {
                    case "tbody":
                        // Tbody element, we should analyze its children for trows
                        double tbodyWidth = AnalyzeTbodyStructure(htmlChildNode, columnStarts, activeRowSpans, tableWidth);
                        if (tbodyWidth > tableWidth)
                        {
                            // Table width must be increased to supported newly added wide row
                            tableWidth = tbodyWidth;
                        }
                        else if (tbodyWidth == 0)
                        {
                            // Tbody analysis may return 0, probably due to unprocessable format. 
                            // We should also fail.
                            columnWidthsAvailable = false; // interrupt the analisys
                        }
                        break;
                    case "tr":
                        // Table row. Analyze column structure within row directly
                        double trWidth = AnalyzeTRStructure(htmlChildNode, columnStarts, activeRowSpans, tableWidth);
                        if (trWidth > tableWidth)
                        {
                            tableWidth = trWidth;
                        }
                        else if (trWidth == 0)
                        {
                            columnWidthsAvailable = false; // interrupt the analisys
                        }
                        break;
                    case "td":
                        // Incorrect formatting, too deep to analyze at this level. Return null.
                        // TODO: implement analysis at this level, possibly by creating a new tr
                        columnWidthsAvailable = false; // interrupt the analisys
                        break;
                    default:
                        // Element should not occur directly in table. Ignore it.
                        break;
                }

                htmlChildNode = htmlChildNode.NextSibling;
            }

            if (columnWidthsAvailable)
            {
                // Add an item for whole table width
                columnStarts.Add(tableWidth);
                VerifyColumnStartsAscendingOrder(columnStarts);
            }
            else
            {
                columnStarts = null;
            }

            return columnStarts;
        }

        /// <summary>
        /// Performs a parsing pass over a tbody to read information about column width and rowspan attributes. Information read about width
        /// attributes is stored in the reference ArrayList parameter columnStarts, which contains a list of all starting
        /// positions of all columns in the table, ordered from left to right. Row spans are taken into consideration when 
        /// computing column starts
        /// </summary>
        /// <param name="htmlTbodyElement">
        /// XmlElement representing Html tbody whose structure is to be analyzed
        /// </param>
        /// <param name="columnStarts">
        /// ArrayList of type double which contains the function output. If analysis fails, this parameter is set to null
        /// </param>
        /// <param name="tableWidth">
        /// Current width of the table. This is used to determine if a new column when added to the end of table should
        /// come after the last column in the table or is actually splitting the last column in two. If it is only splitting
        /// the last column it should inherit row span for that column
        /// </param>
        /// <returns>
        /// Calculated width of a tbody.
        /// In case of non-analizable column width structure return 0;
        /// </returns>
        private static double AnalyzeTbodyStructure(IXmlNode htmlTbodyElement, List<double> columnStarts, List<int> activeRowSpans, double tableWidth)
        {
            // Parameter validation
            Debug.Assert(htmlTbodyElement.LocalName.ToLower() == "tbody");
            Debug.Assert(columnStarts != null);

            double tbodyWidth = 0;
            bool columnWidthsAvailable = true;

            if (!htmlTbodyElement.HasChildNodes())
            {
                return tbodyWidth;
            }

            // Set active row spans to 0 - thus ignoring row spans crossing tbody boundaries
            ClearActiveRowSpans(activeRowSpans);

            var htmlChildNode = htmlTbodyElement.FirstChild;
          
            // Analyze tr elements
            while (htmlChildNode != null && columnWidthsAvailable)
            {
                switch (htmlChildNode.LocalName.ToLower())
                {
                    case "tr":
                        double trWidth = AnalyzeTRStructure(htmlChildNode, columnStarts, activeRowSpans, tbodyWidth);
                        if (trWidth > tbodyWidth)
                        {
                            tbodyWidth = trWidth;
                        }
                        break;
                    case "td":
                        columnWidthsAvailable = false; // interrupt the analisys
                        break;
                    default:
                        break;
                }

                htmlChildNode = htmlChildNode.NextSibling;
            }

            // Set active row spans to 0 - thus ignoring row spans crossing tbody boundaries
            ClearActiveRowSpans(activeRowSpans);

            return columnWidthsAvailable ? tbodyWidth : 0;
        }

        /// <summary>
        /// Performs a parsing pass over a tr element to read information about column width and rowspan attributes.  
        /// </summary>
        /// <param name="htmlTRElement">
        /// XmlElement representing Html tr element whose structure is to be analyzed
        /// </param>
        /// <param name="columnStarts">
        /// ArrayList of type double which contains the function output. If analysis is successful, this ArrayList contains
        /// all the points which are the starting position of any column in the tr, ordered from left to right. If analysis fails,
        /// the ArrayList is set to null
        /// </param>
        /// <param name="activeRowSpans">
        /// ArrayList representing all columns currently spanned by an earlier row span attribute. These columns should
        /// not be used for data in this row. The ArrayList actually contains notation for all columns in the table, if the
        /// active row span is set to 0 that column is not presently spanned but if it is > 0 the column is presently spanned
        /// </param>
        /// <param name="tableWidth">
        /// Double value representing the current width of the table.
        /// Return 0 if analisys was insuccessful.
        /// </param>
        private static double AnalyzeTRStructure(IXmlNode htmlTRElement, List<double> columnStarts, List<int> activeRowSpans, double tableWidth)
        {
            double columnWidth;

            // Parameter validation
            Debug.Assert(htmlTRElement.LocalName.ToLower() == "tr");
            Debug.Assert(columnStarts != null);
            Debug.Assert(activeRowSpans != null);
            Debug.Assert(columnStarts.Count == activeRowSpans.Count);

            if (!htmlTRElement.HasChildNodes())
            {
                return 0;
            }

            bool columnWidthsAvailable = true;

            double columnStart = 0; // starting position of current column
            var htmlChildNode = htmlTRElement.FirstChild;
            int columnIndex = 0;
            double trWidth = 0;

            // Skip spanned columns to get to real column start
            if (columnIndex < activeRowSpans.Count)
            {
                Debug.Assert((double)columnStarts[columnIndex] >= columnStart);
                if ((double)columnStarts[columnIndex] == columnStart)
                {
                    // The new column may be in a spanned area
                    while (columnIndex < activeRowSpans.Count && (int)activeRowSpans[columnIndex] > 0)
                    {
                        activeRowSpans[columnIndex] = (int)activeRowSpans[columnIndex] - 1;
                        Debug.Assert((int)activeRowSpans[columnIndex] >= 0);
                        columnIndex++;
                        columnStart = (double)columnStarts[columnIndex];
                    }
                }
            }

            while (htmlChildNode != null && columnWidthsAvailable)
            {
                Debug.Assert(columnStarts.Count == activeRowSpans.Count);

                VerifyColumnStartsAscendingOrder(columnStarts);

                switch (htmlChildNode.LocalName.ToLower())
                {
                    case "td":
                        Debug.Assert(columnIndex <= columnStarts.Count);
                        if (columnIndex < columnStarts.Count)
                        {
                            Debug.Assert(columnStart <= (double)columnStarts[columnIndex]);
                            if (columnStart < (double)columnStarts[columnIndex])
                            {
                                columnStarts.Insert(columnIndex, columnStart);
                                // There can be no row spans now - the column data will appear here
                                // Row spans may appear only during the column analysis
                                activeRowSpans.Insert(columnIndex, 0);
                            }
                        }
                        else
                        {
                            // Column start is greater than all previous starts. Row span must still be 0 because
                            // we are either adding after another column of the same row, in which case it should not inherit
                            // the previous column's span. Otherwise we are adding after the last column of some previous
                            // row, and assuming the table widths line up, we should not be spanned by it. If there is
                            // an incorrect tbale structure where a columns starts in the middle of a row span, we do not
                            // guarantee correct output
                            columnStarts.Add(columnStart);
                            activeRowSpans.Add(0);
                        }
                        columnWidth = GetColumnWidth(htmlChildNode);
                        if (columnWidth != -1)
                        {
                            int nextColumnIndex;
                            int rowSpan = GetRowSpan(htmlChildNode);

                            nextColumnIndex = GetNextColumnIndex(columnIndex, columnWidth, columnStarts, activeRowSpans);
                            if (nextColumnIndex != -1)
                            {
                                // Entire column width can be processed without hitting conflicting row span. This means that
                                // column widths line up and we can process them
                                Debug.Assert(nextColumnIndex <= columnStarts.Count);

                                // Apply row span to affected columns
                                for (int spannedColumnIndex = columnIndex; spannedColumnIndex < nextColumnIndex; spannedColumnIndex++)
                                {
                                    activeRowSpans[spannedColumnIndex] = rowSpan - 1;
                                    Debug.Assert((int)activeRowSpans[spannedColumnIndex] >= 0);
                                }

                                columnIndex = nextColumnIndex;

                                // Calculate columnsStart for the next cell
                                columnStart = columnStart + columnWidth;

                                if (columnIndex < activeRowSpans.Count)
                                {
                                    Debug.Assert((double)columnStarts[columnIndex] >= columnStart);
                                    if ((double)columnStarts[columnIndex] == columnStart)
                                    {
                                        // The new column may be in a spanned area
                                        while (columnIndex < activeRowSpans.Count && (int)activeRowSpans[columnIndex] > 0)
                                        {
                                            activeRowSpans[columnIndex] = (int)activeRowSpans[columnIndex] - 1;
                                            Debug.Assert((int)activeRowSpans[columnIndex] >= 0);
                                            columnIndex++;
                                            columnStart = (double)columnStarts[columnIndex];
                                        }
                                    }
                                    // else: the new column does not start at the same time as a pre existing column
                                    // so we don't have to check it for active row spans, it starts in the middle
                                    // of another column which has been checked already by the GetNextColumnIndex function
                                }
                            }
                            else
                            {
                                // Full column width cannot be processed without a pre existing row span.
                                // We cannot analyze widths
                                columnWidthsAvailable = false;
                            }
                        }
                        else
                        {
                            // Incorrect column width, stop processing
                            columnWidthsAvailable = false;
                        }
                        break;
                    default:
                        break;
                }

                htmlChildNode = htmlChildNode.NextSibling;
            }

            // The width of the tr element is the position at which it's last td element ends, which is calculated in
            // the columnStart value after each td element is processed
            if (columnWidthsAvailable)
            {
                trWidth = columnStart;
            }
            else
            {
                trWidth = 0;
            }

            return trWidth;
        }

        /// <summary>
        /// Gets row span attribute from htmlTDElement. Returns an integer representing the value of the rowspan attribute.
        /// Default value if attribute is not specified or if it is invalid is 1
        /// </summary>
        /// <param name="htmlTDElement">
        /// Html td element to be searched for rowspan attribute
        /// </param>
        private static int GetRowSpan(IXmlNode htmlTDElement)
        {
            string rowSpanAsString;
            int rowSpan;

            rowSpanAsString = GetAttribute(htmlTDElement, "rowspan");
            if (rowSpanAsString != null)
            {
                if (!Int32.TryParse(rowSpanAsString, out rowSpan))
                {
                    // Ignore invalid value of rowspan; treat it as 1
                    rowSpan = 1;
                }
            }
            else
            {
                // No row span, default is 1
                rowSpan = 1;
            }
            return rowSpan;
        }

        /// <summary>
        /// Gets index at which a column should be inseerted into the columnStarts ArrayList. This is
        /// decided by the value columnStart. The columnStarts ArrayList is ordered in ascending order.
        /// Returns an integer representing the index at which the column should be inserted
        /// </summary>
        /// <param name="columnStarts">
        /// Array list representing starting coordinates of all columns in the table
        /// </param>
        /// <param name="columnStart">
        /// Starting coordinate of column we wish to insert into columnStart
        /// </param>
        /// <param name="columnIndex">
        /// Int representing the current column index. This acts as a clue while finding the insertion index.
        /// If the value of columnStarts at columnIndex is the same as columnStart, then this position alrady exists
        /// in the array and we can jsut return columnIndex.
        /// </param>
        /// <returns></returns>
        private static int GetNextColumnIndex(int columnIndex, double columnWidth, List<double> columnStarts, List<int> activeRowSpans)
        {
            double columnStart;
            int spannedColumnIndex;

            // Parameter validation
            Debug.Assert(columnStarts != null);
            Debug.Assert(0 <= columnIndex && columnIndex <= columnStarts.Count);
            Debug.Assert(columnWidth > 0);

            columnStart = (double)columnStarts[columnIndex];
            spannedColumnIndex = columnIndex + 1;

            while (spannedColumnIndex < columnStarts.Count && (double)columnStarts[spannedColumnIndex] < columnStart + columnWidth && spannedColumnIndex != -1)
            {
                if ((int)activeRowSpans[spannedColumnIndex] > 0)
                {
                    // The current column should span this area, but something else is already spanning it
                    // Not analyzable
                    spannedColumnIndex = -1;
                }
                else
                {
                    spannedColumnIndex++;
                }
            }

            return spannedColumnIndex;
        }

        
        /// <summary>
        /// Used for clearing activeRowSpans array in the beginning/end of each tbody
        /// </summary>
        /// <param name="activeRowSpans">
        /// ArrayList representing currently active row spans
        /// </param>
        private static void ClearActiveRowSpans(List<int> activeRowSpans)
        {
            for (int columnIndex = 0; columnIndex < activeRowSpans.Count; columnIndex++)
            {
                activeRowSpans[columnIndex] = 0;
            }
        }

        /// <summary>
        /// Used for initializing activeRowSpans array in the before adding rows to tbody element
        /// </summary>
        /// <param name="activeRowSpans">
        /// ArrayList representing currently active row spans
        /// </param>
        /// <param name="count">
        /// Size to be give to array list
        /// </param>
        private static void InitializeActiveRowSpans(List<int> activeRowSpans, int count)
        {
            for (int columnIndex = 0; columnIndex < count; columnIndex++)
            {
                activeRowSpans.Add(0);
            }
        }


        /// <summary>
        /// Calculates width of next TD element based on starting position of current element and it's width, which
        /// is calculated byt he function
        /// </summary>
        /// <param name="htmlTDElement">
        /// XmlElement representing Html td element whose width is to be read
        /// </param>
        /// <param name="columnStart">
        /// Starting position of current column
        /// </param>
        private static double GetNextColumnStart(XmlElement htmlTDElement, double columnStart)
        {
            double columnWidth;
            double nextColumnStart;
            
            // Parameter validation
            Debug.Assert(htmlTDElement.LocalName.ToLower() == "td" || htmlTDElement.LocalName.ToLower() == "th");
            Debug.Assert(columnStart >= 0);

            nextColumnStart = -1;  // -1 indicates inability to calculate columnStart width

            columnWidth = GetColumnWidth(htmlTDElement);

            if (columnWidth == -1)
            {
                nextColumnStart = -1;
            }
            else
            {
                nextColumnStart = columnStart + columnWidth;
            }

            return nextColumnStart;
        }


        private static double GetColumnWidth(IXmlNode htmlTDElement)
        {
            string columnWidthAsString;
            double columnWidth;

            columnWidthAsString = null;
            columnWidth = -1;

            // Get string valkue for the width
            columnWidthAsString = GetAttribute(htmlTDElement, "width");
            if (columnWidthAsString == null)
            {
                columnWidthAsString = GetCssAttribute(GetAttribute(htmlTDElement, "style"), "width");
            }

            // We do not allow column width to be 0, if specified as 0 we will fail to record it
            if (!TryGetLengthValue(columnWidthAsString, out columnWidth) || columnWidth == 0)
            {
                columnWidth = -1;
            }
            return columnWidth;
        }

        /// <summary>
        /// Calculates column span based the column width and the widths of all other columns. Returns an integer representing 
        /// the column span
        /// </summary>
        /// <param name="columnIndex">
        /// Index of the current column
        /// </param>
        /// <param name="columnWidth">
        /// Width of the current column
        /// </param>
        /// <param name="columnStarts">
        /// ArrayList repsenting starting coordinates of all columns
        /// </param>
        private static int CalculateColumnSpan(int columnIndex, double columnWidth, List<double> columnStarts)
        {
            // Current status of column width. Indicates the amount of width that has been scanned already
            double columnSpanningValue;
            int columnSpanningIndex;
            int columnSpan;
            double subColumnWidth; // Width of the smallest-grain columns in the table

            Debug.Assert(columnStarts != null);
            Debug.Assert(columnIndex < columnStarts.Count - 1);
            Debug.Assert((double)columnStarts[columnIndex] >= 0);
            Debug.Assert(columnWidth > 0);

            columnSpanningIndex = columnIndex;
            columnSpanningValue = 0;
            columnSpan = 0;
            subColumnWidth = 0;

            while (columnSpanningValue <  columnWidth && columnSpanningIndex < columnStarts.Count - 1)
            {
                subColumnWidth = (double)columnStarts[columnSpanningIndex + 1] - (double)columnStarts[columnSpanningIndex];
                Debug.Assert(subColumnWidth > 0);
                columnSpanningValue += subColumnWidth;
                columnSpanningIndex++;
            }

            // Now, we have either covered the width we needed to cover or reached the end of the table, in which
            // case the column spans all the columns until the end
            columnSpan = columnSpanningIndex - columnIndex;
            Debug.Assert(columnSpan > 0);

            return columnSpan;
        }

        /// <summary>
        /// Verifies that values in columnStart, which represent starting coordinates of all columns, are arranged
        /// in ascending order
        /// </summary>
        /// <param name="columnStarts">
        /// ArrayList representing starting coordinates of all columns
        /// </param>
        private static void VerifyColumnStartsAscendingOrder(List<double> columnStarts)
        {
            Debug.Assert(columnStarts != null);

            double columnStart;

            columnStart = -0.01;

            for (int columnIndex = 0; columnIndex < columnStarts.Count; columnIndex++)
            {
                Debug.Assert(columnStart < (double)columnStarts[columnIndex]);
                columnStart = (double)columnStarts[columnIndex];
            }
        }

        // .............................................................
        //
        // Attributes and Properties
        //
        // .............................................................

        /// <summary>
        /// Analyzes local properties of Html element, converts them into Xaml equivalents, and applies them to xamlElement
        /// </summary>
        /// <param name="xamlElement">
        /// XmlElement representing Xaml element to which properties are to be applied
        /// </param>
        /// <param name="localProperties">
        /// Dictionary<string,string> representing local properties of Html element that is converted into xamlElement
        /// </param>
        private static void ApplyLocalProperties(XmlElement xamlElement, Dictionary<string,string> localProperties, bool isBlock)
        {
            bool marginSet = false;
            string marginTop = "0";
            string marginBottom = "0";
            string marginLeft = "0";
            string marginRight = "0";

            bool paddingSet = false;
            string paddingTop = "0";
            string paddingBottom = "0";
            string paddingLeft = "0";
            string paddingRight = "0";

            string borderColor = null;

            bool borderThicknessSet = false;
            string borderThicknessTop = "0";
            string borderThicknessBottom = "0";
            string borderThicknessLeft = "0";
            string borderThicknessRight = "0";

            var propertyEnumerator = localProperties.GetEnumerator();

            while (propertyEnumerator.MoveNext())
            {
                switch ((string)propertyEnumerator.Current.Key)
                {
                    case "font-family":
                        //  Convert from font-family value list into xaml FontFamily value
                        xamlElement.SetAttribute(Xaml_FontFamily, (string)propertyEnumerator.Current.Value);
                        break;
                    case "font-style":
                        xamlElement.SetAttribute(Xaml_FontStyle, (string)propertyEnumerator.Current.Value);
                        break;
                    case "font-variant":
                        //  Convert from font-variant into xaml property
                        break;
                    case "font-weight":
                        xamlElement.SetAttribute(Xaml_FontWeight, (string)propertyEnumerator.Current.Value);
                        break;
                    case "font-size":
                        //  Convert from css size into FontSize
                        xamlElement.SetAttribute(Xaml_FontSize, (string)propertyEnumerator.Current.Value);
                        break;
                    case "color":
                        // XXX
                        //SetPropertyValue(xamlElement, TextElement.ForegroundProperty, (string)propertyEnumerator.Value);
                        break;
                    case "background-color":
                        // XXX
                        //SetPropertyValue(xamlElement, TextElement.BackgroundProperty, (string)propertyEnumerator.Value);
                        break;
                    case "text-decoration-underline":
                        if (!isBlock)
                        {
                            if ((string)propertyEnumerator.Current.Value == "true")
                            {
                                xamlElement.SetAttribute(Xaml_TextDecorations, Xaml_TextDecorations_Underline);
                            }
                        }
                        break;
                    case "text-decoration-none":
                    case "text-decoration-overline":
                    case "text-decoration-line-through":
                    case "text-decoration-blink":
                        //  Convert from all other text-decorations values
                        if (!isBlock)
                        {
                        }
                        break;
                    case "text-transform":
                        //  Convert from text-transform into xaml property
                        break;

                    case "text-indent":
                        if (isBlock)
                        {
                            xamlElement.SetAttribute(Xaml_TextIndent, (string)propertyEnumerator.Current.Value);
                        }
                        break;

                    case "text-align":
                        if (isBlock)
                        {
                            xamlElement.SetAttribute(Xaml_TextAlignment, (string)propertyEnumerator.Current.Value);
                        }
                        break;

                    case "width":
                    case "height":
                        //  Decide what to do with width and height propeties
                        break;

                    case "margin-top":
                        marginSet = true;
                        marginTop = (string)propertyEnumerator.Current.Value;
                        break;
                    case "margin-right":
                        marginSet = true;
                        marginRight = (string)propertyEnumerator.Current.Value;
                        break;
                    case "margin-bottom":
                        marginSet = true;
                        marginBottom = (string)propertyEnumerator.Current.Value;
                        break;
                    case "margin-left":
                        marginSet = true;
                        marginLeft = (string)propertyEnumerator.Current.Value;
                        break;

                    case "padding-top":
                        paddingSet = true;
                        paddingTop = (string)propertyEnumerator.Current.Value;
                        break;
                    case "padding-right":
                        paddingSet = true;
                        paddingRight = (string)propertyEnumerator.Current.Value;
                        break;
                    case "padding-bottom":
                        paddingSet = true;
                        paddingBottom = (string)propertyEnumerator.Current.Value;
                        break;
                    case "padding-left":
                        paddingSet = true;
                        paddingLeft = (string)propertyEnumerator.Current.Value;
                        break;

                    // NOTE: css names for elementary border styles have side indications in the middle (top/bottom/left/right)
                    // In our internal notation we intentionally put them at the end - to unify processing in ParseCssRectangleProperty method
                    case "border-color-top":
                        borderColor = (string)propertyEnumerator.Current.Value;
                        break;
                    case "border-color-right":
                        borderColor = (string)propertyEnumerator.Current.Value;
                        break;
                    case "border-color-bottom":
                        borderColor = (string)propertyEnumerator.Current.Value;
                        break;
                    case "border-color-left":
                        borderColor = (string)propertyEnumerator.Current.Value;
                        break;
                    case "border-style-top":
                    case "border-style-right":
                    case "border-style-bottom":
                    case "border-style-left":
                        //  Implement conversion from border style
                        break;
                    case "border-width-top":
                        borderThicknessSet = true;
                        borderThicknessTop = (string)propertyEnumerator.Current.Value;
                        break;
                    case "border-width-right":
                        borderThicknessSet = true;
                        borderThicknessRight = (string)propertyEnumerator.Current.Value;
                        break;
                    case "border-width-bottom":
                        borderThicknessSet = true;
                        borderThicknessBottom = (string)propertyEnumerator.Current.Value;
                        break;
                    case "border-width-left":
                        borderThicknessSet = true;
                        borderThicknessLeft = (string)propertyEnumerator.Current.Value;
                        break;

                    case "list-style-type":
                        if ((string)xamlElement.LocalName == Xaml_List)
                        {
                            string markerStyle;
                            switch (((string)propertyEnumerator.Current.Value).ToLower())
                            {
                                case "disc":
                                    markerStyle = HtmlToXamlConverter.Xaml_List_MarkerStyle_Disc;
                                    break;
                                case "circle":
                                    markerStyle = HtmlToXamlConverter.Xaml_List_MarkerStyle_Circle;
                                    break;
                                case "none":
                                    markerStyle = HtmlToXamlConverter.Xaml_List_MarkerStyle_None;
                                    break;
                                case "square":
                                    markerStyle = HtmlToXamlConverter.Xaml_List_MarkerStyle_Square;
                                    break;
                                case "box":
                                    markerStyle = HtmlToXamlConverter.Xaml_List_MarkerStyle_Box;
                                    break;
                                case "lower-latin":
                                    markerStyle = HtmlToXamlConverter.Xaml_List_MarkerStyle_LowerLatin;
                                    break;
                                case "upper-latin":
                                    markerStyle = HtmlToXamlConverter.Xaml_List_MarkerStyle_UpperLatin;
                                    break;
                                case "lower-roman":
                                    markerStyle = HtmlToXamlConverter.Xaml_List_MarkerStyle_LowerRoman;
                                    break;
                                case "upper-roman":
                                    markerStyle = HtmlToXamlConverter.Xaml_List_MarkerStyle_UpperRoman;
                                    break;
                                case "decimal":
                                    markerStyle = HtmlToXamlConverter.Xaml_List_MarkerStyle_Decimal;
                                    break;
                                default:
                                    markerStyle = HtmlToXamlConverter.Xaml_List_MarkerStyle_Disc;
                                    break;
                            }
                            xamlElement.SetAttribute(HtmlToXamlConverter.Xaml_List_MarkerStyle, markerStyle);
                        }
                        break;

                    case "float":
                    case "clear":
                        if (isBlock)
                        {
                            //  Convert float and clear properties
                        }
                        break;

                    case "display":
                        break;
                }
            }

            if (isBlock)
            {
                if (marginSet)
                {
                    ComposeThicknessProperty(xamlElement, Xaml_Margin, marginLeft, marginRight, marginTop, marginBottom);
                }

                if (paddingSet)
                {
                    ComposeThicknessProperty(xamlElement, Xaml_Padding, paddingLeft, paddingRight, paddingTop, paddingBottom);
                }

                if (borderColor != null)
                {
                    //  We currently ignore possible difference in brush colors on different border sides. Use the last colored side mentioned
                    xamlElement.SetAttribute(Xaml_BorderBrush, borderColor);
                }

                if (borderThicknessSet)
                {
                    ComposeThicknessProperty(xamlElement, Xaml_BorderThickness, borderThicknessLeft, borderThicknessRight, borderThicknessTop, borderThicknessBottom);
                }
            }
        }

        // Create syntactically optimized four-value Thickness
        private static void ComposeThicknessProperty(XmlElement xamlElement, string propertyName, string left, string right, string top, string bottom)
        {
            // Xaml syntax:
            // We have a reasonable interpreation for one value (all four edges), two values (horizontal, vertical),
            // and four values (left, top, right, bottom).
            //  switch (i) {
            //    case 1: return new Thickness(lengths[0]);
            //    case 2: return new Thickness(lengths[0], lengths[1], lengths[0], lengths[1]);
            //    case 4: return new Thickness(lengths[0], lengths[1], lengths[2], lengths[3]);
            //  }
            string thickness;

            // We do not accept negative margins
            if (left[0] == '0' || left[0] == '-') left = "0";
            if (right[0] == '0' || right[0] == '-') right = "0";
            if (top[0] == '0' || top[0] == '-') top = "0";
            if (bottom[0] == '0' || bottom[0] == '-') bottom = "0";

            if (left == right && top == bottom)
            {
                if (left == top)
                {
                    thickness = left;
                }
                else
                {
                    thickness = left + "," + top;
                }
            }
            else
            {
                thickness = left + "," + top + "," + right + "," + bottom;
            }

            //  Need safer processing for a thickness value
            xamlElement.SetAttribute(propertyName, thickness);
        }

        //private static void SetPropertyValue(XmlElement xamlElement, DependencyProperty property, string stringValue)
        //{
        //    System.ComponentModel.TypeConverter typeConverter = System.ComponentModel.TypeDescriptor.GetConverter(property.PropertyType);

        //    try
        //    {
        //        object convertedValue = typeConverter.ConvertFromInvariantString(stringValue);
            
        //        if (convertedValue != null)
        //        {
        //            xamlElement.SetAttribute(property.Name, stringValue);
        //        }
        //    }
        //    catch(Exception)
        //    {
        //    }
        //}

        /// <summary>
        /// Analyzes the tag of the htmlElement and infers its associated formatted properties.
        /// After that parses style attribute and adds all inline css styles.
        /// The resulting style attributes are collected in output parameter localProperties.
        /// </summary>
        /// <param name="htmlElement">
        /// </param>
        /// <param name="inheritedProperties">
        /// set of properties inherited from ancestor elements. Currently not used in the code. Reserved for the future development.
        /// </param>
        /// <param name="localProperties">
        /// returns all formatting properties defined by this element - implied by its tag, its attributes, or its css inline style
        /// </param>
        /// 
        /// <param name="sourceContext"></param>
        /// <returns>
        /// returns a combination of previous context with local set of properties.
        /// This value is not used in the current code - inntended for the future development.
        /// </returns>
        private static Dictionary<string, string> GetElementProperties(IXmlNode htmlElement, Dictionary<string, string> inheritedProperties, out Dictionary<string, string> localProperties, List<IXmlNode> sourceContext)
        {
            // Start with context formatting properties
            Dictionary<string,string> currentProperties = new Dictionary<string,string>();

            var propertyEnumerator = inheritedProperties.GetEnumerator();
            
            while (propertyEnumerator.MoveNext())
            {
                currentProperties[propertyEnumerator.Current.Key] = propertyEnumerator.Current.Value;
            }

            // Identify element name
            string elementName = ((string)htmlElement.LocalName).ToLower();
            string elementNamespace = (string)htmlElement.NamespaceUri;

            // update current formatting properties depending on element tag

            localProperties = new Dictionary<string,string>();
            switch (elementName)
            {
                // Character formatting
                case "i":
                case "italic":
                case "em":
                    localProperties["font-style"] = "italic";
                    break;
                case "b":
                case "bold":
                case "strong":
                case "dfn":
                    localProperties["font-weight"] = "bold";
                    break;
                case "u":
                case "underline":
                    localProperties["text-decoration-underline"] = "true";
                    break;
                case "font":
                    string attributeValue = GetAttribute(htmlElement, "face");
                    if (attributeValue != null)
                    {
                        localProperties["font-family"] = attributeValue;
                    }
                    attributeValue = GetAttribute(htmlElement, "size");
                    if (attributeValue != null)
                    {
                        double fontSize = double.Parse(attributeValue) * (12.0 / 3.0);
                        if (fontSize < 1.0)
                        {
                            fontSize = 1.0;
                        }
                        else if (fontSize > 1000.0)
                        {
                            fontSize = 1000.0;
                        }
                        localProperties["font-size"] = fontSize.ToString();
                    }
                    attributeValue = GetAttribute(htmlElement, "color");
                    if (attributeValue != null)
                    {
                        localProperties["color"] = attributeValue;
                    }
                    break;
                case "samp":
                    localProperties["font-family"] = "Courier New"; // code sample
                    localProperties["font-size"] = Xaml_FontSize_XXSmall;
                    localProperties["text-align"] = "Left";
                    break;
                case "sub":
                    break;
                case "sup":
                    break;

                // Hyperlinks
                case "a": // href, hreflang, urn, methods, rel, rev, title
                    //  Set default hyperlink properties
                    break;
                case "acronym":
                    break;

                // Paragraph formatting:
                case "p":
                    //  Set default paragraph properties
                    break;
                case "div":
                    //  Set default div properties
                    break;
                case "pre":
                    localProperties["font-family"] = "Courier New"; // renders text in a fixed-width font
                    localProperties["font-size"] = Xaml_FontSize_XXSmall;
                    localProperties["text-align"] = "Left";
                    break;
                case "blockquote":
                    localProperties["margin-left"] = "16";
                    break;

                case "h1":
                    localProperties["font-size"] = Xaml_FontSize_XXLarge;
                    break;
                case "h2":
                    localProperties["font-size"] = Xaml_FontSize_XLarge;
                    break;
                case "h3":
                    localProperties["font-size"] = Xaml_FontSize_Large;
                    break;
                case "h4":
                    localProperties["font-size"] = Xaml_FontSize_Medium;
                    break;
                case "h5":
                    localProperties["font-size"] = Xaml_FontSize_Small;
                    break;
                case "h6":
                    localProperties["font-size"] = Xaml_FontSize_XSmall;
                    break;
                // List properties
                case "ul":
                    localProperties["list-style-type"] = "disc";
                    break;
                case "ol":
                    localProperties["list-style-type"] = "decimal";
                    break;

                case "table":
                case "body":
                case "html":
                    break;
            }

            // Override html defaults by css attributes - from stylesheets and inline settings
            //HtmlCssParser.GetElementPropertiesFromCssAttributes(htmlElement, elementName, stylesheet, localProperties, sourceContext);

            // Combine local properties with context to create new current properties
            propertyEnumerator = localProperties.GetEnumerator();

            while (propertyEnumerator.MoveNext())
            {
                currentProperties[propertyEnumerator.Current.Key] = propertyEnumerator.Current.Value;
            }

            return currentProperties;
        }

        /// <summary>
        /// Extracts a value of css attribute from css style definition.
        /// </summary>
        /// <param name="cssStyle">
        /// Source csll style definition
        /// </param>
        /// <param name="attributeName">
        /// A name of css attribute to extract
        /// </param>
        /// <returns>
        /// A string rrepresentation of an attribute value if found;
        /// null if there is no such attribute in a given string.
        /// </returns>
        private static string GetCssAttribute(string cssStyle, string attributeName)
        {
            //  This is poor man's attribute parsing. Replace it by real css parsing
            if (cssStyle != null)
            {
                string[] styleValues;

                attributeName = attributeName.ToLower();

                // Check for width specification in style string
                styleValues = cssStyle.Split(';');

                for (int styleValueIndex = 0; styleValueIndex < styleValues.Length; styleValueIndex++)
                {
                    string[] styleNameValue;

                    styleNameValue = styleValues[styleValueIndex].Split(':');
                    if (styleNameValue.Length == 2)
                    {
                        if (styleNameValue[0].Trim().ToLower() == attributeName)
                        {
                            return styleNameValue[1].Trim();
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Converts a length value from string representation to a double.
        /// </summary>
        /// <param name="lengthAsString">
        /// Source string value of a length.
        /// </param>
        /// <param name="length"></param>
        /// <returns></returns>
        private static bool TryGetLengthValue(string lengthAsString, out double length)
        {
            length = Double.NaN;

            if (lengthAsString != null)
            {
                lengthAsString = lengthAsString.Trim().ToLower();

                // We try to convert currentColumnWidthAsString into a double. This will eliminate widths of type "50%", etc.
                if (lengthAsString.EndsWith("pt"))
                {
                    lengthAsString = lengthAsString.Substring(0, lengthAsString.Length - 2);
                    if (Double.TryParse(lengthAsString, out length))
                    {
                        length = (length * 96.0) / 72.0; // convert from points to pixels
                    }
                    else
                    {
                        length = Double.NaN;
                    }
                }
                else if (lengthAsString.EndsWith("px"))
                {
                    lengthAsString = lengthAsString.Substring(0, lengthAsString.Length - 2);
                    if (!Double.TryParse(lengthAsString, out length))
                    {
                        length = Double.NaN;
                    }
                }
                else
                {
                    if (!Double.TryParse(lengthAsString, out length)) // Assuming pixels
                    {
                        length = Double.NaN;
                    }
                }
            }

            return !Double.IsNaN(length);
        }

        // .................................................................
        //
        // Pasring Color Attribute
        //
        // .................................................................

        private static string GetColorValue(string colorValue)
        {
            // TODO: Implement color conversion
            return colorValue;
        }

        /// <summary>
        /// Applies properties to xamlTableCellElement based on the html td element it is converted from.
        /// </summary>
        /// <param name="htmlChildNode">
        /// Html td/th element to be converted to xaml
        /// </param>
        /// <param name="xamlTableCellElement">
        /// XmlElement representing Xaml element for which properties are to be processed
        /// </param>
        /// <remarks>
        /// TODO: Use the processed properties for htmlChildNode instead of using the node itself 
        /// </remarks>
        private static void ApplyPropertiesToTableCellElement(IXmlNode htmlChildNode, IXmlNode xamlTableCellElement)
        {
            // Parameter validation
            Debug.Assert(htmlChildNode.LocalName.ToLower() == "td" || htmlChildNode.LocalName.ToLower() == "th");
            Debug.Assert((string)xamlTableCellElement.LocalName == Xaml_TableCell);

            var element = xamlTableCellElement as XmlElement;

            // set default border thickness for xamlTableCellElement to enable gridlines
            element.SetAttribute(Xaml_TableCell_BorderThickness, "1,1,1,1");
            element.SetAttribute(Xaml_TableCell_BorderBrush, Xaml_Brushes_Black);
            
            string rowSpanString = GetAttribute(htmlChildNode, "rowspan");
            
            if (rowSpanString != null)
            {
                element.SetAttribute(Xaml_TableCell_RowSpan, rowSpanString);
            }
        }

        #endregion Private Methods

        // ----------------------------------------------------------------
        //
        // Internal Constants
        //
        // ----------------------------------------------------------------

        // The constants represent all Xaml names used in a conversion
        public const string Xaml_FlowDocument = "FlowDocument";

        public const string Xaml_Run = "Run";
        public const string Xaml_Span = "Span";
        public const string Xaml_Hyperlink = "Hyperlink";
        public const string Xaml_Hyperlink_NavigateUri = "NavigateUri";
        public const string Xaml_Hyperlink_TargetName = "TargetName";

        public const string Xaml_Section = "Section";

        public const string Xaml_List = "List";

        public const string Xaml_List_MarkerStyle = "MarkerStyle";
        public const string Xaml_List_MarkerStyle_None = "None";
        public const string Xaml_List_MarkerStyle_Decimal = "Decimal";
        public const string Xaml_List_MarkerStyle_Disc = "Disc";
        public const string Xaml_List_MarkerStyle_Circle = "Circle";
        public const string Xaml_List_MarkerStyle_Square = "Square";
        public const string Xaml_List_MarkerStyle_Box = "Box";
        public const string Xaml_List_MarkerStyle_LowerLatin = "LowerLatin";
        public const string Xaml_List_MarkerStyle_UpperLatin = "UpperLatin";
        public const string Xaml_List_MarkerStyle_LowerRoman = "LowerRoman";
        public const string Xaml_List_MarkerStyle_UpperRoman = "UpperRoman";

        public const string Xaml_ListItem = "ListItem";

        public const string Xaml_LineBreak = "LineBreak";

        public const string Xaml_Paragraph = "Paragraph";

        public const string Xaml_Margin = "Margin";
        public const string Xaml_Padding = "Padding";
        public const string Xaml_BorderBrush = "BorderBrush";
        public const string Xaml_BorderThickness = "BorderThickness";

        public const string Xaml_Table = "Table";

        public const string Xaml_TableColumn = "TableColumn";
        public const string Xaml_TableRowGroup = "TableRowGroup";
        public const string Xaml_TableRow = "TableRow";

        public const string Xaml_TableCell = "TableCell";
        public const string Xaml_TableCell_BorderThickness = "BorderThickness";
        public const string Xaml_TableCell_BorderBrush = "BorderBrush";

        public const string Xaml_TableCell_ColumnSpan = "ColumnSpan";
        public const string Xaml_TableCell_RowSpan = "RowSpan";

        public const string Xaml_Width = "Width";
        public const string Xaml_Brushes_Black = "Black";
        public const string Xaml_FontFamily = "FontFamily";

        public const string Xaml_FontSize = "FontSize";
        public const string Xaml_FontSize_XXLarge = "22pt"; // "XXLarge";
        public const string Xaml_FontSize_XLarge  = "20pt"; // "XLarge";
        public const string Xaml_FontSize_Large   = "18pt"; // "Large";
        public const string Xaml_FontSize_Medium  = "16pt"; // "Medium";
        public const string Xaml_FontSize_Small   = "12pt"; // "Small";
        public const string Xaml_FontSize_XSmall  = "10pt"; // "XSmall";
        public const string Xaml_FontSize_XXSmall = "8pt"; // "XXSmall";

        public const string Xaml_FontWeight = "FontWeight";
        public const string Xaml_FontWeight_Bold = "Bold";

        public const string Xaml_FontStyle = "FontStyle";

        public const string Xaml_Foreground = "Foreground";
        public const string Xaml_Background = "Background";
        public const string Xaml_TextDecorations = "TextDecorations";
        public const string Xaml_TextDecorations_Underline = "Underline";

        public const string Xaml_TextIndent = "TextIndent";
        public const string Xaml_TextAlignment = "TextAlignment";

        // ---------------------------------------------------------------------
        //
        // Private Fields
        //
        // ---------------------------------------------------------------------

        #region Private Fields

        static string _xamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        #endregion Private Fields
    }
}