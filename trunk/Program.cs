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
using System.Reflection;
using System.Text;
using CSharpCLI.Argument;
using CSharpCLI.Help;
using CSharpCLI.Parse;
using StyleCop;
using StyleCopCLI.Properties;
using VSFile;
using VSFile.Project;
using VSFile.Source;

namespace StyleCopCLI
{
	/// <summary>
	///		<para>
	///		Command-line interface to StyleCop source code analyzers.
	///		</para>
	///		<para>
	///		Useful for integrating StyleCop into custom build environments.
	///		</para>
	/// </summary>
	public static class Program
	{
		/// <summary>
		/// Names of switches to parse from command-line arguments.
		/// </summary>
		static class SwitchNames
		{
			public const string ConfigurationFlags = "flags";
			public const string Help = "?";
			public const string OutputFile = "out";
			public const string ProjectFiles = "proj";
			public const string RecursiveSearch = "r";
			public const string SettingsFile = "set";
			public const string SolutionFiles = "sln";
			public const string SourceFiles = "cs";
		}

		/// <summary>
		/// URL containing files used in this application.
		/// </summary>
		const string Url = "http://sourceforge.net/projects/stylecopcli";

		////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// Unique key assigned to each CodeProject object.
		/// </summary>
		static int s_codeProjectKey;

		/// <summary>
		/// Code projects containing source code files to analyze.
		/// </summary>
		static List<CodeProject> s_codeProjects;

		/// <summary>
		/// Configuration containing flags to use during analysis.
		/// </summary>
		static Configuration s_configuration;

		/// <summary>
		/// StyleCop console used for source code analysis.
		/// </summary>
		static StyleCopConsole s_console;

		/// <summary>
		/// Command-line argument parser.
		/// </summary>
		static ArgumentParser s_parser;

		/// <summary>
		/// Expected switches to parse from command-line arguments.
		/// </summary>
		static SwitchCollection s_switches;

		////////////////////////////////////////////////////////////////////////
		// Methods

		/// <summary>
		/// Add Visual C# project files to list of code projects.
		/// </summary>
		static void AddProjectFiles()
		{
			if (Parser.IsParsed(SwitchNames.ProjectFiles))
			{
				string[] filePaths = Parser.GetValues(SwitchNames.ProjectFiles);

				VisualStudioFiles files = new VisualStudioFiles(filePaths, RecursiveSearch);

				AddProjectFiles(files.CSharpProjectFiles);
			}
		}

		/// <summary>
		/// Add given Visual C# project files to list of code projects.
		/// </summary>
		/// <param name="projectFiles">
		/// Enumerable collection of CSharpProjectFile objects representing
		/// Visual C# project files to add.
		/// </param>
		static void AddProjectFiles(IEnumerable<CSharpProjectFile> projectFiles)
		{
			foreach (CSharpProjectFile projectFile in projectFiles)
			{
				CodeProject codeProject = CreateCodeProject(projectFile.DirectoryPath);

				projectFile.Load();

				foreach (CSharpSourceFile sourceFile in projectFile.SourceFiles)
					AddSourceFile(sourceFile, codeProject);

				CodeProjects.Add(codeProject);
			}
		}

		/// <summary>
		/// Add Visual Studio solution files to list of code projects.
		/// </summary>
		static void AddSolutionFiles()
		{
			if (Parser.IsParsed(SwitchNames.SolutionFiles))
			{
				string[] filePaths = Parser.GetValues(SwitchNames.SolutionFiles);

				VisualStudioFiles files = new VisualStudioFiles(filePaths, RecursiveSearch);

				foreach (SolutionFile solutionFile in files.SolutionFiles)
				{
					solutionFile.Load();

					AddProjectFiles(solutionFile.CSharpProjectFiles);
				}
			}
		}

		/// <summary>
		/// Add given Visual C# source file to given code project.
		/// </summary>
		/// <param name="sourceFile">
		/// CSharpSourceFile representing Visual C# source file to add.
		/// </param>
		/// <param name="codeProject">
		/// Code project to add Visual C# source file to.
		/// </param>
		static void AddSourceFile(CSharpSourceFile sourceFile,
			CodeProject codeProject)
		{
			sourceFile.Load();

			Analyzer.Core.Environment.AddSourceCode(codeProject,
				sourceFile.FilePath, null);
		}

		/// <summary>
		/// Add Visual C# source files to list of code projects.
		/// </summary>
		static void AddSourceFiles()
		{
			if (Parser.IsParsed(SwitchNames.SourceFiles))
			{
				string[] filePaths = Parser.GetValues(SwitchNames.SourceFiles);

				VisualStudioFiles files = new VisualStudioFiles(filePaths, RecursiveSearch);

				foreach (CSharpSourceFile sourceFile in files.CSharpSourceFiles)
				{
					CodeProject codeProject = CreateCodeProject(sourceFile.DirectoryPath);

					AddSourceFile(sourceFile, codeProject);

					CodeProjects.Add(codeProject);
				}
			}
		}

		/// <summary>
		/// Start source code analysis.
		/// </summary>
		static void Analyze()
		{
			// Determine if no source code files to analyze.
			if (!HasCodeProjects)
			{
				Console.WriteLine(Resources.NoFilesToAnalyze);

				return;
			}

			Analyzer.Start(CodeProjects, true);

			// Clean up console.

			Analyzer.OutputGenerated -= OnOutputGenerated;
			Analyzer.ViolationEncountered -= OnViolationEncountered;
		}

		/// <summary>
		/// Create code project given directory path.
		/// </summary>
		/// <param name="directoryPath">
		/// String representing path to directory containing code project source
		/// files.
		/// </param>
		/// <returns>
		/// Code project representing source files contained in common directory.
		/// </returns>
		static CodeProject CreateCodeProject(string directoryPath)
		{
			return new CodeProject(NextCodeProjectKey, directoryPath, Configuration);
		}

		/// <summary>
		/// Define expected switches to parse from command-line arguments.
		/// </summary>
		static void DefineSwitches()
		{
			s_switches = new SwitchCollection();

			s_switches.Add(SwitchNames.Help,
				Resources.HelpSwitchLongName,
				Resources.HelpSwitchDescription);

			s_switches.Add(SwitchNames.ConfigurationFlags,
				Resources.ConfigurationFlagsSwitchLongName,
				Resources.ConfigurationFlagsSwitchDescription,
				true,
				false,
				Resources.ConfigurationFlagsSwitchArgumentName);

			s_switches.Add(SwitchNames.OutputFile,
				Resources.OutputFileSwitchLongName,
				Resources.OutputFileSwitchDescription,
				false,
				Resources.OutputFileSwitchArgumentName);

			s_switches.Add(SwitchNames.ProjectFiles,
				Resources.ProjectFilesSwitchLongName,
				Resources.ProjectFilesSwitchDescription,
				true,
				false,
				Resources.ProjectFilesSwitchArgumentName);

			s_switches.Add(SwitchNames.RecursiveSearch,
				Resources.RecursiveSearchSwitchLongName,
				Resources.RecursiveSearchSwitchDescription);

			s_switches.Add(SwitchNames.SettingsFile,
				Resources.SettingsFileSwitchLongName,
				Resources.SettingsFileSwitchDescription,
				false,
				Resources.SettingsFileSwitchArgumentName);

			s_switches.Add(SwitchNames.SolutionFiles,
				Resources.SolutionFilesSwitchLongName,
				Resources.SolutionFilesSwitchDescription,
				true,
				false,
				Resources.SolutionFilesSwitchArgumentName);

			s_switches.Add(SwitchNames.SourceFiles,
				Resources.SourceFilesSwitchLongName,
				Resources.SourceFilesSwitchDescription,
				true,
				false,
				Resources.SourceFilesSwitchArgumentName);
		}

		/// <summary>
		/// Get header to print on help screen.
		/// </summary>
		/// <returns>
		/// String representing header to print on help screen.
		/// </returns>
		static string GetHeader()
		{
			const char Space = ' ';

			AssemblyTitleAttribute title =
				AssemblyTitleAttribute.GetCustomAttribute(Assembly,
				typeof(AssemblyTitleAttribute)) as AssemblyTitleAttribute;

			AssemblyCopyrightAttribute copyright =
				AssemblyCopyrightAttribute.GetCustomAttribute(Assembly,
				typeof(AssemblyCopyrightAttribute)) as AssemblyCopyrightAttribute;

			StringBuilder header = new StringBuilder();

			header.AppendLine(title.Title + Space + Version);
			header.AppendLine(copyright.Copyright);
			header.Append(Url);

			return header.ToString();
		}

		/// <summary>
		/// Initialize StyleCop console with command-line argument values.
		/// </summary>
		static void InitializeConsole()
		{
			string settingsFile = Parser.GetValue(SwitchNames.SettingsFile);
			string outputFile = Parser.GetValue(SwitchNames.OutputFile);

			s_console = new StyleCopConsole(settingsFile, true, outputFile, null, true);

			s_configuration = new Configuration(null);

			if (Parser.IsParsed(SwitchNames.ConfigurationFlags))
			{
				string[] flags = Parser.GetValues(SwitchNames.ConfigurationFlags);

				s_configuration = new Configuration(flags);
			}

			s_codeProjects = new List<CodeProject>();

			s_codeProjectKey = 0;

			AddProjectFiles();
			AddSolutionFiles();
			AddSourceFiles();

			if (HasCodeProjects)
			{
				Analyzer.OutputGenerated += OnOutputGenerated;
				Analyzer.ViolationEncountered += OnViolationEncountered;
			}
		}

		/// <summary>
		/// Main entry into program that supplies command-line arguments.
		/// </summary>
		/// <param name="arguments">
		/// Array of strings representing command-line arguments.
		/// </param>
		static void Main(string[] arguments)
		{
			try
			{
				bool printHelp = ParseArguments(arguments);

				if (printHelp)
				{
					PrintHelp();

					return;
				}

				InitializeConsole();

				Analyze();
			}
			catch (Exception exception)
			{
				Console.WriteLine(exception.Message);
			}
		}

		/// <summary>
		/// Initialize command-line argument parser and parse given
		/// command-line arguments.
		/// </summary>
		/// <param name="arguments">
		/// Array of strings representing command-line arguments to parse.
		/// </param>
		/// <returns>
		/// True if help should be printed, false otherwise.
		/// </returns>
		static bool ParseArguments(string[] arguments)
		{
			DefineSwitches();

			s_parser = new ArgumentParser(arguments, Switches);

			s_parser.Parse();

			if (s_parser.IsParsed(SwitchNames.Help) ||
				s_parser.NoneParsed(SwitchNames.ProjectFiles,
				SwitchNames.SolutionFiles, SwitchNames.SourceFiles))
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Print help for using this executable, including switch usage
		/// information and descriptions.
		/// </summary>
		static void PrintHelp()
		{
			HelpPrinter helpPrinter = new HelpPrinter(ExecutableName, Switches,
				GetHeader());

			helpPrinter.Print();
		}

		////////////////////////////////////////////////////////////////////////
		// Events

		/// <summary>
		/// Fired when output generated during analysis.
		/// </summary>
		/// <param name="sender">
		/// Object representing source of event.
		/// </param>
		/// <param name="e">
		/// OutputEventArgs representing output generated event data.
		/// </param>
		static void OnOutputGenerated(object sender, OutputEventArgs e)
		{
			Console.WriteLine(e.Output);
		}

		/// <summary>
		/// Fired when violation encountered in source code files being analyzed.
		/// </summary>
		/// <param name="sender">
		/// Object representing source of event.
		/// </param>
		/// <param name="e">
		/// ViolationEventArgs representing violation event data.
		/// </param>
		static void OnViolationEncountered(object sender, ViolationEventArgs e)
		{
			// Note: To be used for generating custom violation reports.
		}

		////////////////////////////////////////////////////////////////////////
		// Properties

		/// <summary>
		/// Get StyleCop console used for source code analysis.
		/// </summary>
		/// <value>
		/// StyleCopConsole representing StyleCop console to use for analysis.
		/// </value>
		static StyleCopConsole Analyzer
		{
			get { return s_console; }
		}

		/// <summary>
		/// Get executing assembly.
		/// </summary>
		/// <value>
		/// Assembly representing executing assembly.
		/// </value>
		static Assembly Assembly
		{
			get { return Assembly.GetExecutingAssembly(); }
		}

		/// <summary>
		/// Get code projects containing source code files to analyze.
		/// </summary>
		/// <value>
		/// List of CodeProject objects containing source code files to analyze.
		/// </value>
		static IList<CodeProject> CodeProjects
		{
			get { return s_codeProjects; }
		}

		/// <summary>
		/// Get configuration containing flags to use during analysis.
		/// </summary>
		/// <value>
		/// Configuration containing flags to use during analysis.
		/// </value>
		static Configuration Configuration
		{
			get { return s_configuration; }
		}

		/// <summary>
		/// Get executable name.
		/// </summary>
		/// <value>
		/// String representing executable name.
		/// </value>
		static string ExecutableName
		{
			get { return Assembly.GetName().Name; }
		}

		/// <summary>
		/// Determine if any code projects exist containing source code files
		/// to analyze.
		/// </summary>
		/// <value>
		/// True if any code projects exist, false otherwise.
		/// </value>
		static bool HasCodeProjects
		{
			get { return CodeProjects != null && CodeProjects.Count > 0; }
		}

		/// <summary>
		/// Get next unique key assigned to CodeProject objects.
		/// </summary>
		/// <value>
		/// Integer representing unique key to assign to CodeProject objects.
		/// </value>
		static int NextCodeProjectKey
		{
			get { return s_codeProjectKey++; }
		}

		/// <summary>
		/// Get command-line argument parser.
		/// </summary>
		/// <value>
		/// ArgumentParser representing command-line argument parser.
		/// </value>
		static ArgumentParser Parser
		{
			get { return s_parser; }
		}

		/// <summary>
		/// Determine if recursive search of files to analyze specified.
		/// </summary>
		/// <value>
		/// True if recursive search specified, false otherwise.
		/// </value>
		static bool RecursiveSearch
		{
			get { return Parser.IsParsed(SwitchNames.RecursiveSearch); }
		}

		/// <summary>
		/// Get expected switches to parse from command-line arguments.
		/// </summary>
		/// <value>
		/// SwitchCollection containing expected switches to parse from
		/// command-line arguments.
		/// </value>
		static SwitchCollection Switches
		{
			get { return s_switches; }
		}

		/// <summary>
		/// Get executable version.
		/// </summary>
		/// <value>
		/// String representing executable version.
		/// </value>
		static string Version
		{
			get { return Assembly.GetName().Version.ToString(); }
		}
	}
}
