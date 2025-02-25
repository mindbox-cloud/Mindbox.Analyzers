using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MindboxAnalyzers.Rules;

public class KafkaAdminClientCreateTopicsAndPartitionsProhibitedRule : AnalyzerRule, ISemanticModelAnalyzerRule
{
	public KafkaAdminClientCreateTopicsAndPartitionsProhibitedRule()
		: base(
			ruleId: "Mindbox2006",
			title: "Forbids the use of IAdminClient.CreateTopicsAsync and CreatePartitionsAsync.",
			messageFormat: "Cannot use IAdminClient.CreateTopicsAsync or CreatePartitionsAsync. Use IKafkaTopicsManager from Mindbox.Kafka to account for partition limits and ensure SLO.",
			description: "Using IAdminClient.CreateTopicsAsync and CreatePartitionsAsync is prohibited because these methods do not enforce partition limits required for SLO. Instead, use IKafkaTopicsManager from Mindbox.Kafka."
		)
	{
	}

	public void AnalyzeModel(SemanticModel model, out ICollection<Diagnostic> foundProblems)
	{
		var forbiddenMethods = new[] { "CreateTopicsAsync", "CreatePartitionsAsync" };

		foundProblems = model.SyntaxTree
			.GetRoot()
			.DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.Where(invocation =>
			{
				if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
					return false;

				if (!forbiddenMethods.Contains(methodSymbol.Name))
					return false;

				return methodSymbol.ContainingType?.Name == "IAdminClient";
			})
			.Select(invocation => CreateDiagnosticForLocation(invocation.GetLocation()))
			.ToList();
	}
}