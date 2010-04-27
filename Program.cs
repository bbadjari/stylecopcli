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
using Microsoft.StyleCop;
using StyleCopCLI.InputFile;

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
			public const string SettingsFile = "set";
			public const string SolutionFiles = "sln";
		}

		/// <summary>
		/// URL containing files used in this application.
		/// </summary>
		const string Url = "http://sourceforge.net/projects/stylecopcli";

		////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// StyleCop console used for source code analysis.
		/// </summary>
		static StyleCopConsole s_console;

		/// <summary>
		/// Command-line argument parser.
		/// </summary>
		static ArgumentParser s_parser;

		/// <summary>
		/// Unique key assigned to each CodeProject object.
		/// </summary>
		static int s_projectKey;

		/// <summary>
		/// Projects containing source code files to analyze.
		/// </summary>
		static List<CodeProject> s_projects;

		/// <summary>
		/// Expected switches to parse from command-line arguments.
		/// </summary>
		static SwitchCollection s_switches;

		////////////////////////////////////////////////////////////////////////
		// Methods

		/// <summary>
		/// Add C# project files to given list of projects.
		/// </summary>
		/// <param name="configuration">
		/// Configuration containing flags to use during analysis.
		/// </param>
		/// <param name="projects">
		/// List of projects to add C# project files to.
		/// </param>
		static void AddProjectFiles(Configuration configuration,
			List<CodeProject> projects)
		{
			if (Parser.IsParsed(SwitchNames.ProjectFiles))
			{
				string[] filePaths = Parser.GetValues(SwitchNames.ProjectFiles);

				List<CSharpProjectFile> projectFiles = new List<CSharpProjectFile>();

				foreach (string filePath in filePaths)
				{
					CSharpProjectFile projectFile = new CSharpProjectFile(filePath);

					projectFiles.Add(projectFile);
				}

				AddProjectFiles(projectFiles, configuration, projects);
			}
		}

		/// <summary>
		/// Add given C# project files to given list of projects.
		/// </summary>
		/// <param name="projectFiles">
		/// Enumerable collection of CSharpProjectFile objects representing
		/// C# project files to add.
		/// </param>
		/// <param name="configuration">
		/// Configuration containing flags to use during analysis.
		/// </param>
		/// <param name="projects">
		/// List of projects to add given C# project files to.
		/// </param>
		static void AddProjectFiles(IEnumerable<CSharpProjectFile> projectFiles,
			Configuration configuration, List<CodeProject> projects)
		{
			foreach (CSharpProjectFile projectFile in projectFiles)
			{
				CodeProject project = new CodeProject(NextProjectKey,
					projectFile.DirectoryPath, configuration);

				projectFile.Load();

				foreach (string filePath in projectFile.SourceFilePaths)
					Analyzer.Core.Environment.AddSourceCode(project, filePath, null);

				projects.Add(project);
			}
		}

		/// <summary>
		/// Add Visual Studio solution files to given list of projects.
		/// </summary>
		/// <param name="configuration">
		/// Configuration containing flags to use during analysis.
		/// </param>
		/// <param name="projects">
		/// List of projects to add Visual Studio solution files to.
		/// </param>
		static void AddSolutionFiles(Configuration configuration,
			List<CodeProject> projects)
		{
			if (Parser.IsParsed(SwitchNames.SolutionFiles))
			{
				string[] filePaths = Parser.GetValues(SwitchNames.SolutionFiles);

				foreach (string filePath in filePaths)
				{
					SolutionFile solutionFile = new SolutionFile(filePath);

					solutionFile.Load();

					AddProjectFiles(solutionFile.ProjectFiles, configuration,
						projects);
				}
			}
		}

		/// <summary>
		/// Start source code analysis.
		/// </summary>
		static void Analyze()
		{
			Analyzer.Start(Projects, true);

			// Clean up console.

			Analyzer.OutputGenerated -= OnOutputGenerated;
			Analyzer.ViolationEncountered -= OnViolationEncountered;

			Analyzer.Dispose();
		}

		/// <summary>
		/// Define expected switches to parse from command-line arguments.
		/// </summary>
		static void DefineSwitches()
		{
			s_switches = new SwitchCollection();

			s_switches.Add(SwitchNames.Help,
				"help",
				"Print this help screen.");

			s_switches.Add(SwitchNames.ConfigurationFlags,
				"configurationFlags",
				"Configuration flags to use during analysis (e.g. DEBUG, RELEASE).",
				true,
				false,
				"flags");

			s_switches.Add(SwitchNames.OutputFile,
				"outputFile",
				"Output file to write analysis results to.",
				false,
				"filePath");

			s_switches.Add(SwitchNames.ProjectFiles,
				"projectFiles",
				"Visual C# project files to analyze.",
				true,
				false,
				"filePaths");

			s_switches.Add(SwitchNames.SettingsFile,
				"settingsFile",
				"StyleCop settings file to use during analysis.",
				false,
				"filePath");

			s_switches.Add(SwitchNames.SolutionFiles,
				"solutionFiles",
				"Visual Studio solution files to analyze.",
				true,
				false,
				"filePaths");
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

			Configuration configuration = new Configuration(null);

			if (Parser.IsParsed(SwitchNames.ConfigurationFlags))
			{
				string[] flags = Parser.GetValues(SwitchNames.ConfigurationFlags);

				configuration = new Configuration(flags);
			}

			s_projects = new List<CodeProject>();

			s_projectKey = 0;

			AddSolutionFiles(configuration, s_projects);

			AddProjectFiles(configuration, s_projects);

			Analyzer.OutputGenerated += OnOutputGenerated;
			Analyzer.ViolationEncountered += OnViolationEncountered;
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
				s_parser.NoneParsed(SwitchNames.SolutionFiles, SwitchNames.ProjectFiles))
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
		/// Get next unique key assigned to CodeProject objects.
		/// </summary>
		/// <value>
		/// Integer representing unique key to assign to CodeProject objects.
		/// </value>
		static int NextProjectKey
		{
			get { return s_projectKey++; }
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
		/// Get projects containing source code files to analyze.
		/// </summary>
		/// <value>
		/// List of CodeProject objects containing source code files to analyze.
		/// </value>
		static IList<CodeProject> Projects
		{
			get { return s_projects; }
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
