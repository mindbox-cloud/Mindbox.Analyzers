using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MindboxAnalyzers.Rules;

public class GroupByEntityRule : AnalyzerRule, ISemanticModelAnalyzerRule
{
	private const string RuleId = "Mindbox2005";
	private const string Title =
		"Forbids grouping by properties of type with [Table] attribute.";
	private const string MessageFormat =
		"Grouping by properties of type with [Table] attribute is not allowed.";
	private const string Description =
		"Grouping by properties of type with [Table] attribute is not allowed to avoid issues with xmin/xid in Postgres (see https://github.com/npgsql/efcore.pg/issues/3202).";

	public GroupByEntityRule()
		: base(
			ruleId: RuleId,
			title: Title,
			messageFormat: MessageFormat,
			description: Description,
			isEnabledByDefault: false)
	{
	}

	public void AnalyzeModel(SemanticModel model, out ICollection<Diagnostic> foundProblems)
	{
		foundProblems = new List<Diagnostic>();

		var invocationExpressions = model.SyntaxTree
			.GetRoot()
			.DescendantNodes()
			.OfType<InvocationExpressionSyntax>();

		foreach (var invocationExpression in invocationExpressions)
		{
			if (invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccessExpr)
			{
				continue;
			}

			if (model.GetSymbolInfo(memberAccessExpr).Symbol is not IMethodSymbol memberSymbol
				|| memberSymbol.ContainingNamespace.ToString() != "System.Linq"
				|| memberSymbol.ContainingType.Name != "Queryable"
				|| memberSymbol.Name != "GroupBy")
			{
				continue;
			}

			var argumentList = invocationExpression.ArgumentList.Arguments;
			if (argumentList.Count == 0)
			{
				continue;
			}

			if (argumentList[0].Expression is not SimpleLambdaExpressionSyntax lambdaExpr)
			{
				continue;
			}

			if (lambdaExpr.Body is not MemberAccessExpressionSyntax groupByProperty)
			{
				continue;
			}

			if (model.GetSymbolInfo(groupByProperty).Symbol is not IPropertySymbol propertySymbol)
			{
				continue;
			}

			var propertyTypeSymbol = propertySymbol.Type;
			var attributes = propertyTypeSymbol.GetAttributes();

			if (attributes.Any(x =>
				x.AttributeClass is { } attributeClass
				&& attributeClass.ContainingNamespace.ToString() == "System.ComponentModel.DataAnnotations.Schema"
				&& attributeClass.Name == "TableAttribute"))
			{
				var diagnostic = CreateDiagnosticForLocation(groupByProperty.GetLocation());
				foundProblems.Add(diagnostic);
			}
		}
	}
}