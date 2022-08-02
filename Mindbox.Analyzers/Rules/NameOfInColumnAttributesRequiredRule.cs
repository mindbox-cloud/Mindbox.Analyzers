using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MindboxAnalyzers.Rules;

public class NameOfInColumnAttributesRequiredRule : AnalyzerRule, ITreeAnalyzerRule
{
	public NameOfInColumnAttributesRequiredRule()
		: base(
			ruleId: "Mindbox2002",
			title: "Instead of specifying the field name as a string nameof operator should be used",
			messageFormat: "Use nameof instead of specifying the field name",
			description: "Instead of specifying the field name as a string nameof operator should be used")
	{
	}

	public void AnalyzeTree(SyntaxTree tree, out ICollection<Diagnostic> foundProblems)
	{
		foundProblems = tree
			.GetRoot()
			.DescendantNodes(node => !node.IsKind(SyntaxKind.MethodDeclaration))
			.OfType<PropertyDeclarationSyntax>()
			.SelectMany(property => property.AttributeLists
				.SelectMany(list => list.Attributes
					.Where(attr => attr.Name.ToString() is "Column" or "Association")
					.Where(attr => attr.ArgumentList?.Arguments != null)))
			.SelectMany(columnAttribute => columnAttribute
				.ArgumentList
				.Arguments
				.Where(argument => (argument.NameEquals?.Name.Identifier.Text is "Storage" or "ThisKey")
					|| (argument.NameEquals is null && (argument.NameColon is null || argument.NameColon.Name.Identifier.Text == "name"))))
			.Where(storageArgument => storageArgument != null)
			.Where(storageArgument => storageArgument.Expression.IsKind(SyntaxKind.StringLiteralExpression))
			.Select(storageArgument => CreateDiagnosticForLocation(storageArgument.Expression.GetLocation()))
			.ToList();
	}
}