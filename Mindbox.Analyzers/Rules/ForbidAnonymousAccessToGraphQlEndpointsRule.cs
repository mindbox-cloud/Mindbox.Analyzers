using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MindboxAnalyzers.Rules;

public class ForbidAnonymousAccessToGraphQlEndpointsRule : AnalyzerRule, ISemanticModelAnalyzerRule
{
	private const string RuleId = "Mindbox2007";
	private const string Title = "Forbids anonymous access to GraphQL endpoints.";
	private const string Message = "GraphQL endpoints must not allow anonymous access. Use RequireAuthorization() instead of AllowAnonymous().";
	private const string Description =
		"GraphQL endpoints must be closed for anonymous access. When mapping a GraphQL endpoint, do not call AllowAnonymous(); use RequireAuthorization() instead.";

	public ForbidAnonymousAccessToGraphQlEndpointsRule()
		: base(
			ruleId: RuleId,
			title: Title,
			messageFormat: Message,
			description: Description)
	{
	}

	public void AnalyzeModel(SemanticModel model, out ICollection<Diagnostic> foundProblems)
	{
		foundProblems = model.SyntaxTree
			.GetRoot()
			.DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.Where(invocation => IsAnonymousGraphQlEndpointInvocation(invocation, model))
			.Select(invocation => CreateDiagnosticForLocation(invocation.GetLocation()))
			.ToList();
	}

	private static bool IsAnonymousGraphQlEndpointInvocation(InvocationExpressionSyntax invocation, SemanticModel model)
	{
		if (!IsMethodNamed(invocation, "AllowAnonymous"))
			return false;

		if (GetReceiverExpression(invocation) is not { } receiverExpression)
			return false;

		return IsGraphQlEndpointExpression(receiverExpression, model, new HashSet<ISymbol>(SymbolEqualityComparer.Default));
	}

	private static bool IsGraphQlEndpointExpression(
		ExpressionSyntax expression,
		SemanticModel model,
		HashSet<ISymbol> visitedSymbols)
	{
		expression = Unwrap(expression);

		switch (expression)
		{
			case InvocationExpressionSyntax invocation:
				if (IsMethodNamed(invocation, "MapGraphQL"))
					return true;

				return GetReceiverExpression(invocation) is { } invocationReceiver
					&& IsGraphQlEndpointExpression(invocationReceiver, model, visitedSymbols);

			case IdentifierNameSyntax:
			case MemberAccessExpressionSyntax:
				return TryResolveAssignedGraphQlEndpoint(expression, model, visitedSymbols);

			case ConditionalAccessExpressionSyntax conditionalAccess:
				return IsGraphQlEndpointExpression(conditionalAccess.Expression, model, visitedSymbols);

			default:
				return false;
		}
	}

	private static bool TryResolveAssignedGraphQlEndpoint(
		ExpressionSyntax expression,
		SemanticModel model,
		HashSet<ISymbol> visitedSymbols)
	{
		var symbol = model.GetSymbolInfo(expression).Symbol;
		if (symbol == null || !visitedSymbols.Add(symbol))
			return false;

		foreach (var assignedExpression in GetAssignedExpressions(symbol, expression.SyntaxTree.GetRoot(), model))
		{
			if (IsGraphQlEndpointExpression(assignedExpression, model, visitedSymbols))
				return true;
		}

		return false;
	}

	private static IEnumerable<ExpressionSyntax> GetAssignedExpressions(ISymbol symbol, SyntaxNode root, SemanticModel model)
	{
		foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
		{
			var syntax = syntaxReference.GetSyntax();
			switch (syntax)
			{
				case VariableDeclaratorSyntax variableDeclarator when variableDeclarator.Initializer != null:
					yield return variableDeclarator.Initializer.Value;
					break;
				case PropertyDeclarationSyntax propertyDeclaration when propertyDeclaration.Initializer != null:
					yield return propertyDeclaration.Initializer.Value;
					break;
				case FieldDeclarationSyntax fieldDeclaration:
					foreach (var variable in fieldDeclaration.Declaration.Variables)
					{
						if (variable.Initializer != null
							&& SymbolEqualityComparer.Default.Equals(model.GetDeclaredSymbol(variable), symbol))
						{
							yield return variable.Initializer.Value;
						}
					}
					break;
			}
		}

		foreach (var assignment in root.DescendantNodes().OfType<AssignmentExpressionSyntax>())
		{
			if (!SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(assignment.Left).Symbol, symbol))
				continue;

			yield return assignment.Right;
		}
	}

	private static ExpressionSyntax GetReceiverExpression(InvocationExpressionSyntax invocation)
	{
		return invocation.Expression switch
		{
			MemberAccessExpressionSyntax memberAccess => memberAccess.Expression,
			MemberBindingExpressionSyntax => null,
			IdentifierNameSyntax => null,
			_ => null
		};
	}

	private static ExpressionSyntax Unwrap(ExpressionSyntax expression)
	{
		while (expression is ParenthesizedExpressionSyntax parenthesizedExpression)
		{
			expression = parenthesizedExpression.Expression;
		}

		return expression;
	}

	private static bool IsMethodNamed(InvocationExpressionSyntax invocation, string methodName)
	{
		return TryGetInvokedMethodName(invocation, out var invokedMethodName)
			&& invokedMethodName == methodName;
	}

	private static bool TryGetInvokedMethodName(InvocationExpressionSyntax invocation, out string methodName)
	{
		switch (invocation.Expression)
		{
			case MemberAccessExpressionSyntax memberAccess:
				methodName = memberAccess.Name.Identifier.ValueText;
				return true;
			case IdentifierNameSyntax identifierName:
				methodName = identifierName.Identifier.ValueText;
				return true;
			default:
				methodName = string.Empty;
				return false;
		}
	}
}
