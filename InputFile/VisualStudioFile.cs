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
using System.IO;

namespace StyleCopCLI.InputFile
{
	/// <summary>
	/// Represents a Visual Studio file.
	/// </summary>
	public abstract class VisualStudioFile
	{
		string m_fileExtension;

		string m_filePath;

		/// <summary>
		/// Constructor.
		/// </summary>
		protected VisualStudioFile(string fileExtension, string filePath)
		{
			m_fileExtension = fileExtension;
			m_filePath = filePath;
		}

		////////////////////////////////////////////////////////////////////////
		// Public Methods

		/// <summary>
		/// Load file.
		/// </summary>
		public void Load()
		{
			CheckFilePath();

			CheckFileExtension();

			ReadFile();
		}

		////////////////////////////////////////////////////////////////////////
		// Protected Methods

		/// <summary>
		/// Get full file path including given relative path.
		/// </summary>
		protected string GetFullFilePath(string relativeFilePath)
		{
			return DirectoryPath + Path.DirectorySeparatorChar + relativeFilePath;
		}

		/// <summary>
		/// Read file.
		/// </summary>
		protected abstract void ReadFile();

		////////////////////////////////////////////////////////////////////////
		// Methods

		/// <summary>
		/// Throw exception if file extension is invalid.
		/// </summary>
		void CheckFileExtension()
		{
			string extension = Path.GetExtension(FilePath);

			if (!extension.Equals(FileExtension, StringComparison.OrdinalIgnoreCase))
				throw new IOException("Invalid file extension.");
		}

		/// <summary>
		/// Throw exception if file path is invalid.
		/// </summary>
		void CheckFilePath()
		{
			if (!File.Exists(FilePath))
				throw new FileNotFoundException("File not found at path: " + FilePath);
		}

		////////////////////////////////////////////////////////////////////////
		// Public Properties

		/// <summary>
		/// Get path to directory containing file.
		/// </summary>
		public string DirectoryPath
		{
			get { return Path.GetDirectoryName(FilePath); }
		}

		/// <summary>
		/// Get file path.
		/// </summary>
		public string FilePath
		{
			get { return m_filePath; }
		}

		////////////////////////////////////////////////////////////////////////
		// Properties

		/// <summary>
		/// Get file extension.
		/// </summary>
		string FileExtension
		{
			get { return m_fileExtension; }
		}
	}
}
