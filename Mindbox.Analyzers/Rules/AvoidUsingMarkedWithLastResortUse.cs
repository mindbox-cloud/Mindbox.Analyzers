using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MindboxAnalyzers.Rules;

public class AvoidUsingMarkedWithLastResortUse : AnalyzerRule, ISyntaxNodeAnalyzerRule
{
	public AvoidUsingMarkedWithLastResortUse()
		: base(
			ruleId: "Mindbox1027",
			title: "Avoid using symbols, marked with LastResortUse attribute",
			messageFormat: "Avoid using symbols, marked with LastResortUse attribute",
			description: "Avoid using symbols, marked with LastResortUse attribute",
			severity: DiagnosticSeverity.Error
		)
	{
	}

	public void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context, out ICollection<Diagnostic> foundProblems)
	{
		foundProblems = new List<Diagnostic>();

		var symbol = context.SemanticModel.GetSymbolInfo(context.Node).Symbol;

		if (symbol is null)
			return;

		if (CheckAttributes(symbol.GetAttributes()) || CheckAttributes(symbol.ContainingType.GetAttributes()))
			foundProblems.Add(Diagnostic.Create(DiagnosticDescriptor, context.Node.GetLocation(), symbol.Name));
	}

	private static bool CheckAttributes(ImmutableArray<AttributeData> attributes)
	{
		return attributes.Any(attribute => attribute.AttributeClass?.Name.Equals(nameof(LastResortUse)) ?? false);
	}
}