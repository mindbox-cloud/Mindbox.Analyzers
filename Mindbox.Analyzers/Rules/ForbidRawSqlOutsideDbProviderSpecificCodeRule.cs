using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MindboxAnalyzers.Rules;

public class ForbidRawSqlOutsideDbProviderSpecificCodeRule : AnalyzerRule, ISemanticModelAnalyzerRule
{
	private const string RuleId = "Mindbox2004";
	private const string Title =
		"Forbids raw sql usage outside database provider-specific classes.";
	private const string InvalidNamespaceLinkMessage =
		"Dont use raw sql outside database provider-specific classes. Extract the provider-specific code " +
		"with raw SQL queries into separate classes and configure them through Dependency Injection. " +
		"The class names should start either with 'SqlServer' or 'Postgres'.";
	private const string Description =
		"This rule is intended to prevent the use of raw SQL in shared code when working with a solution " +
		"that uses the Entity Framework and supports SQL Server and PostgreSQL databases. Raw SQL is only valid " +
		"in database provider-specific classes whose names begin with 'SqlServer' or 'Postgres'.";

	private readonly string _dbCommandInterfaceType = typeof(IDbCommand).ToString();

	public ForbidRawSqlOutsideDbProviderSpecificCodeRule()
		: base(
			ruleId: RuleId,
			title: Title,
			messageFormat: InvalidNamespaceLinkMessage,
			description: Description,
			isEnabledByDefault: false)
	{
	}

	public void AnalyzeModel(SemanticModel model, out ICollection<Diagnostic> foundProblems)
	{
		foundProblems = new List<Diagnostic>();

		var memberAccessExpressionSyntaxNodes = model.SyntaxTree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>();

		foreach (var syntaxNode in memberAccessExpressionSyntaxNodes)
		{
			var parentClassName = GetParentClassNode(syntaxNode);

			if (parentClassName.StartsWith("SqlServer") || parentClassName.StartsWith("Postgres"))
				continue;

			var type = model.GetTypeInfo(syntaxNode.Expression).Type;

			if (IsIDbCommandOrDerived(type))
			{
				foundProblems.Add(CreateDiagnosticForLocation(Location.Create(syntaxNode.SyntaxTree, syntaxNode.FullSpan)));
			}
		}
	}

	private bool IsIDbCommandOrDerived(ITypeSymbol typeSymbol)
	{
		if (typeSymbol == null)
		{
			return false;
		}

		if (typeSymbol.ToString() == _dbCommandInterfaceType ||
			typeSymbol.Interfaces.Any(item => item.ToString() == _dbCommandInterfaceType))
		{
			return true;
		}

		return IsIDbCommandOrDerived(typeSymbol.BaseType);
	}

	private string GetParentClassNode(SyntaxNode syntaxNode)
	{
		return syntaxNode switch
		{
			null => string.Empty,
			ClassDeclarationSyntax syntax => syntax.Identifier.ValueText,
			RecordDeclarationSyntax syntax => syntax.Identifier.ValueText,
			_ => GetParentClassNode(syntaxNode.Parent)
		};
	}
}
