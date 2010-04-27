////////////////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2010 Bernard Badjari
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//
////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace StyleCopCLI.InputFile
{
	/// <summary>
	/// Represents a Visual C# project file.
	/// </summary>
	public class CSharpProjectFile : VisualStudioFile
	{
		/// <summary>
		/// XPath expressions used to select elements and attributes in
		/// project file.
		/// </summary>
		const string AutoGenElementXPath = "msb:AutoGen";
		const string CompileElementXPath = "/msb:Project/msb:ItemGroup/msb:Compile";
		const string IncludeAttributeXPath = "@Include";

		/// <summary>
		/// Namespace used in project file.
		/// </summary>
		const string Namespace = "http://schemas.microsoft.com/developer/msbuild/2003";

		/// <summary>
		/// Namespace prefix used in XPath expressions.
		/// </summary>
		const string NamespacePrefix = "msb";

		/// <summary>
		/// File extensions of project and source files.
		/// </summary>
		const string ProjectFileExtension = ".csproj";
		const string SourceFileExtension = ".cs";

		////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// Name of project.
		/// </summary>
		string m_projectName;

		/// <summary>
		/// C# source file paths contained in project file.
		/// </summary>
		List<string> m_sourceFilePaths;

		////////////////////////////////////////////////////////////////////////
		// Constructors

		/// <summary>
		/// Constructor for specifying file path.
		/// </summary>
		/// <param name="filePath">
		/// String representing path to project file.
		/// </param>
		public CSharpProjectFile(string filePath)
			: this(null, filePath)
		{
		}

		/// <summary>
		/// Constructor for specifying project name.
		/// </summary>
		/// <param name="projectName">
		/// String representing project name.
		/// </param>
		/// <param name="filePath">
		/// String representing path to project file.
		/// </param>
		public CSharpProjectFile(string projectName, string filePath)
			: base(ProjectFileExtension, filePath)
		{
			m_projectName = projectName;
			m_sourceFilePaths = new List<string>();
		}

		////////////////////////////////////////////////////////////////////////
		// Protected Methods

		/// <summary>
		/// Read file.
		/// </summary>
		protected override void ReadFile()
		{
			XmlDocument document = new XmlDocument();

			document.Load(FilePath);

			XmlNamespaceManager namespaceManager = new XmlNamespaceManager(document.NameTable);

			namespaceManager.AddNamespace(NamespacePrefix, Namespace);

			XmlNodeList nodeList = document.SelectNodes(CompileElementXPath, namespaceManager);

			foreach (XmlNode node in nodeList)
				AddSourceFile(node, namespaceManager);
		}

		////////////////////////////////////////////////////////////////////////
		// Methods

		/// <summary>
		/// Add source file contained within given XML node and namespace manager.
		/// </summary>
		/// <param name="node">
		/// XmlNode containing source file.
		/// </param>
		/// <param name="namespaceManager">
		/// XmlNamespaceManager managing XML namespace used in project file.
		/// </param>
		void AddSourceFile(XmlNode node, XmlNamespaceManager namespaceManager)
		{
			XmlNode childNode = node.SelectSingleNode(AutoGenElementXPath, namespaceManager);

			if (childNode != null)
			{
				string value = childNode.Value;

				// Ignore auto-generated files.
				if (value != null &&
					value.Equals(Boolean.TrueString, StringComparison.OrdinalIgnoreCase))
				{
					return;
				}
			}

			XmlNode attribute = node.SelectSingleNode(IncludeAttributeXPath, namespaceManager);

			if (attribute != null)
			{
				string value = attribute.Value;

				if (value != null &&
					value.EndsWith(SourceFileExtension, StringComparison.OrdinalIgnoreCase))
				{
					m_sourceFilePaths.Add(GetFullFilePath(value));
				}
			}
		}

		////////////////////////////////////////////////////////////////////////
		// Properties

		/// <summary>
		/// Determine if project has name.
		/// </summary>
		/// <value>
		/// True if project name is specified, false otherwise.
		/// </value>
		public bool HasProjectName
		{
			get { return ProjectName != null; }
		}

		/// <summary>
		/// Get project name.
		/// </summary>
		/// <value>
		/// String representing project name.
		/// </value>
		public string ProjectName
		{
			get { return m_projectName; }
		}

		/// <summary>
		/// Get C# source file paths contained in this project file.
		/// </summary>
		/// <value>
		/// Enumerable collection of strings representing paths to source files
		/// referenced in project file.
		/// </value>
		public IEnumerable<string> SourceFilePaths
		{
			get { return m_sourceFilePaths; }
		}
	}
}
