using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using MindboxAnalyzers.Rules;

namespace MindboxAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MindboxAnalyzer : DiagnosticAnalyzer
{
	private static readonly List<IAnalyzerRule> _rules;
	private static readonly ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics;

	static MindboxAnalyzer()
	{
		_rules = new List<IAnalyzerRule>
		{
			new LineIsTooLongRule(),
			new OnlyTabsShouldBeUsedForIndentationRule(),
			new No3AdjacentEmptyLinesRule(),
			new NoRegionsInsideMethodRule(),
			new CustomUtcDateTimeConvertingRequiredRule(),
			new NoAdjacentWhitespaceAllowedRule(),
			new NoMoreThanOneLineBetweenSimilarDeclarationsRule(),
			new MultiLineMembersShouldBeSeparatedByABlankLineRule(),
			new NameOfInArgumentExceptionsRequiredRule(),
			new NameOfInColumnAttributesRequiredRule(),
			new CacheItemProviderKeyMustBeStaticRule(),
			new NoTestWithoutOwnerRule(),
			new ModelApplicationHostControllerServiceLocatorProhibitedRule(),
			new NamedObjectModelConfigurationRegisterProhibitedRule(),
			new DataContractRequireIfUsingDataMemberRule(),
			new ForbidRawSqlOutsideDbProviderSpecificCodeRule(),
			new ForbidGroupingByNavigationPropertiesRule(),
			new KafkaAdminClientCreateTopicsAndPartitionsProhibitedRule()
		};

		_supportedDiagnostics =
			_rules
				.Select(rule => rule.DiagnosticDescriptor)
				.ToImmutableArray();
	}


	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
		context.RegisterSemanticModelAction(AnalyzeSemanticModel);
	}

	private static void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
	{
		var allDiagnostics = new List<Diagnostic>();

		foreach (var modelAnalyzer in _rules.OfType<ISemanticModelAnalyzerRule>())
		{
			modelAnalyzer.AnalyzeModel(context.SemanticModel, out var foundProblems);
			if (foundProblems != null)
				allDiagnostics.AddRange(foundProblems);
		}

		foreach (var diagnostic in allDiagnostics)
			context.ReportDiagnostic(diagnostic);
	}

	private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
	{
		var allDiagnostics = new List<Diagnostic>();

		var text = context.Tree.GetText(context.CancellationToken);
		foreach (var textAnalyzer in _rules.OfType<ITextAnalyzerRule>())
		{
			textAnalyzer.AnalyzeText(context.Tree, text, out var foundProblems);
			if (foundProblems != null)
				allDiagnostics.AddRange(foundProblems);
		}

		var lineAnalyzers = _rules
			.OfType<ISingleLineAnalyzerRule>()
			.ToList();

		foreach (var line in text.Lines)
		{
			var lineString = line.ToString();
			foreach (var lineAnalyzer in lineAnalyzers)
			{
				lineAnalyzer.AnalyzeLine(context.Tree, line, lineString, out var foundProblems);
				if (foundProblems != null)
					allDiagnostics.AddRange(foundProblems);
			}
		}

		foreach (var treeAnalyzer in _rules.OfType<ITreeAnalyzerRule>())
		{
			treeAnalyzer.AnalyzeTree(context.Tree, out var foundProblems);
			if (foundProblems != null)
				allDiagnostics.AddRange(foundProblems);
		}

		foreach (var diagnostic in allDiagnostics)
			context.ReportDiagnostic(diagnostic);
	}
}