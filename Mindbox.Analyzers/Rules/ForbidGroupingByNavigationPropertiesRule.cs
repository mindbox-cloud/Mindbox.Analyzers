using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MindboxAnalyzers.Rules;

public class ForbidGroupingByNavigationPropertiesRule : AnalyzerRule, ISemanticModelAnalyzerRule
{
	private const string RuleId = "Mindbox2005";

	private const string Title = "Grouping by navigation property check";

	private const string MessageFormat =
		"Grouping by navigation properties is not allowed to avoid issues with generated query. " +
		"Instead, group by key columns of related entity and load it manually after materialization (if necessary)";

	private const string Description =
		"Grouping by navigation properties is not allowed to avoid issues " +
		"with xmin/xid in Postgres (see https://github.com/npgsql/efcore.pg/issues/3202)";

	public ForbidGroupingByNavigationPropertiesRule()
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
			if (!TryGetMethodSymbol(model, invocationExpression, out var methodSymbol)
				|| !IsGroupByMethod(methodSymbol)
				|| !TryGetGroupByProperty(model, invocationExpression, out var groupByProperty)
				|| !HasTableAttribute(groupByProperty.Type))
			{
				continue;
			}

			var diagnostic = CreateDiagnosticForLocation(invocationExpression.GetLocation());
			foundProblems.Add(diagnostic);
		}
	}

	private static bool HasTableAttribute(ITypeSymbol typeSymbol)
	{
		var attributes = typeSymbol.GetAttributes();

		var attributeData = attributes.FirstOrDefault(x =>
			x.AttributeClass is not null
			&& x.AttributeClass.ContainingNamespace.ToString() == "System.ComponentModel.DataAnnotations.Schema"
			&& x.AttributeClass.Name == "TableAttribute");

		return attributeData is not null;
	}

	private static bool TryGetGroupByProperty(
		SemanticModel model,
		InvocationExpressionSyntax invocationExpression,
		out IPropertySymbol groupByProperty)
	{
		groupByProperty = null;

		var argumentList = invocationExpression.ArgumentList.Arguments;
		if (argumentList.Count == 0)
		{
			return false;
		}

		if (argumentList[0].Expression is not SimpleLambdaExpressionSyntax lambdaExpr)
		{
			return false;
		}

		if (lambdaExpr.Body is not MemberAccessExpressionSyntax memberAccessExpression)
		{
			return false;
		}

		if (model.GetSymbolInfo(memberAccessExpression).Symbol is not IPropertySymbol propertySymbol)
		{
			return false;
		}

		groupByProperty = propertySymbol;
		return true;
	}

	private static bool IsGroupByMethod(IMethodSymbol methodSymbol)
	{
		return
			methodSymbol.ContainingNamespace.ToString() == "System.Linq"
			&& methodSymbol.ContainingType.Name == "Queryable"
			&& methodSymbol.Name == "GroupBy";
	}

	private static bool TryGetMethodSymbol(SemanticModel model, InvocationExpressionSyntax invocationExpression, out IMethodSymbol methodSymbol)
	{
		methodSymbol = null;

		if (invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccessExpr)
		{
			return false;
		}

		if (model.GetSymbolInfo(memberAccessExpr).Symbol is not IMethodSymbol memberSymbol)
		{
			return false;
		}

		methodSymbol = memberSymbol;
		return true;
	}
}