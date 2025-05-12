using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MindboxAnalyzers.Rules;

public class DataContractRequireIfUsingDataMemberRule : AnalyzerRule, ISemanticModelAnalyzerRule
{
	public DataContractRequireIfUsingDataMemberRule()
		: base(
			ruleId: "Mindbox2003",
			title: "DataContract attribute must be specified on a class or on its parent if property or field has DataMember attribute",
			messageFormat: "Specify DataContract attribute on a class or on its parent since you have DataMember attribute on property or field",
			description: "DataContract attribute must be specified on a class or on its parent if you have at least one DataMember attribute on field or property")
	{
	}

	public void AnalyzeModel(SemanticModel model, out ICollection<Diagnostic> foundProblems)
	{
		var analyzer = new AttributeAnalyzer(model);

		var classesWithDataMemberAttribute = model
			.SyntaxTree
			.GetRoot()
			.DescendantNodes()
			.OfType<MemberDeclarationSyntax>()
			.Where(member => PropertyOrFieldHasAttribute(member, analyzer) && member.Parent is not null)
			.Select(member => model.GetDeclaredSymbol(member.Parent))
			.Distinct(SymbolEqualityComparer.Default)
			.OfType<ITypeSymbol>()
			.Where(type => !analyzer.ContainsDataContractAttribute(GetFullTypeHierarchyInclusive(type).SelectMany(GetAttributes)));

		foundProblems = classesWithDataMemberAttribute
			.SelectMany(c => c.Locations)
			.Select(CreateDiagnosticForLocation)
			.ToArray();
	}

	private bool PropertyOrFieldHasAttribute(MemberDeclarationSyntax member, AttributeAnalyzer analyzer)
	{
		if (member is not PropertyDeclarationSyntax and not FieldDeclarationSyntax) return false;

		var attributes = analyzer.AttributeToSymbol(FlattenAttributes(member.AttributeLists));
		return analyzer.ContainsDataMemberAttribute(attributes);

	}

	private IEnumerable<INamedTypeSymbol> GetAttributes(ISymbol symbol)
	{
		return symbol.GetAttributes().Select(a => a.AttributeClass);
	}

	private IEnumerable<ITypeSymbol> GetFullTypeHierarchyInclusive(ITypeSymbol mainClass)
	{
		var types = new List<ITypeSymbol>();

		ITypeSymbol currentType = mainClass;
		while (currentType is not null)
		{
			types.Add(currentType);
			currentType = currentType.BaseType;
		}

		return types;
	}

	private IEnumerable<AttributeSyntax> FlattenAttributes(IEnumerable<AttributeListSyntax> attributeLists)
	{
		return attributeLists.SelectMany(l => l.Attributes);
	}

	private class AttributeAnalyzer
	{
		private readonly SemanticModel _model;
		private readonly INamedTypeSymbol _dataMemberAttribute;
		private readonly INamedTypeSymbol _dataContractAttribute;

		public AttributeAnalyzer(SemanticModel model)
		{
			_model = model;
			_dataMemberAttribute = model.Compilation.GetTypeByMetadataName(typeof(DataMemberAttribute).FullName!);
			_dataContractAttribute = model.Compilation.GetTypeByMetadataName(typeof(DataContractAttribute).FullName!);
		}

		public bool ContainsDataMemberAttribute(IEnumerable<INamedTypeSymbol> attributes)
		{
			return ContainsAttribute(attributes, _dataMemberAttribute);
		}

		public bool ContainsDataContractAttribute(IEnumerable<INamedTypeSymbol> attributes)
		{
			return ContainsAttribute(attributes, _dataContractAttribute);
		}

		public IEnumerable<INamedTypeSymbol> AttributeToSymbol(IEnumerable<AttributeSyntax> attributes)
		{
			return attributes.Select(AttributeToSymbol);
		}

		public INamedTypeSymbol AttributeToSymbol(AttributeSyntax attributeSyntax)
		{
			return _model.GetSymbolInfo(attributeSyntax).Symbol?.ContainingType;
		}

		private static bool ContainsAttribute(IEnumerable<INamedTypeSymbol> attributes, INamedTypeSymbol attribute)
		{
			return attributes.Any(a => SymbolEqualityComparer.Default.Equals(a, attribute));
		}
	}
}