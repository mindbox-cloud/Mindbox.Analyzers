using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace MindboxAnalyzers;

public interface IAnalyzerRule
{
	DiagnosticDescriptor DiagnosticDescriptor { get; }
}

public interface ITreeAnalyzerRule : IAnalyzerRule
{
	void AnalyzeTree(SyntaxTree tree, out ICollection<Diagnostic> foundProblems);
}

public interface ITextAnalyzerRule : IAnalyzerRule
{
	void AnalyzeText(SyntaxTree tree, SourceText text, out ICollection<Diagnostic> foundProblems);
}

public interface ISingleLineAnalyzerRule : IAnalyzerRule
{
	/// <summary>
	/// Analyze the line
	/// </summary>
	/// <param name="tree">Syntactic tree</param>
	/// <param name="line">Line number in file</param>
	/// <param name="lineString">Line contents in file</param>
	/// <param name="foundProblems">Collection of found problems. If everything is fine, <c>null</c> is returned.</param>
	void AnalyzeLine(SyntaxTree tree, TextLine line, string lineString, out ICollection<Diagnostic> foundProblems);
}

public interface ISemanticModelAnalyzerRule : IAnalyzerRule
{
	/// <summary>
	/// Analyze the semantic model
	/// </summary>
	/// <param name="model">Semantic model</param>
	/// <param name="foundProblems">Collection of found problems. If everything is fine, <c>null</c> is returned.</param>
	void AnalyzeModel(SemanticModel model, out ICollection<Diagnostic> foundProblems);
}

public interface ISyntaxNodeAnalyzerRule
{
	void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context, out ICollection<Diagnostic> foundProblems);
}

public abstract class AnalyzerRule : IAnalyzerRule
{
	protected const string DefaultCategory = "Mindbox";
	protected const DiagnosticSeverity DefaultSeverity = DiagnosticSeverity.Warning;

	public DiagnosticDescriptor DiagnosticDescriptor { get; }

	protected AnalyzerRule(
		string ruleId,
		string title,
		string messageFormat,
		string description,
		string category = DefaultCategory,
		DiagnosticSeverity severity = DefaultSeverity,
		bool isEnabledByDefault = true)
	{
		DiagnosticDescriptor = new DiagnosticDescriptor(
			ruleId,
			title,
			messageFormat,
			category,
			severity,
			isEnabledByDefault,
			description);
	}

	protected Diagnostic CreateDiagnosticForLocation(Location location)
	{
		return Diagnostic.Create(DiagnosticDescriptor, location);
	}
}