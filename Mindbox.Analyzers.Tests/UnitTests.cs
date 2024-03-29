using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;
using MindboxAnalyzers.Rules;

namespace MindboxAnalyzers.Tests;

[TestClass]
public class UnitTest : CodeFixVerifier
{
	[TestMethod]
	public void NormalLineLength()
	{
		var test = @"class Test {}";

		VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void TooLongLine()
	{
		var test = new string('Z', 131);

		var rule = new LineIsTooLongRule();
		var expected = new DiagnosticResult
		{
			Id = rule.DiagnosticDescriptor.Id,
			Message = rule.DiagnosticDescriptor.MessageFormat.ToString(),
			Severity = DiagnosticSeverity.Warning,
			Locations = new[]
			{
				new DiagnosticResultLocation("Test0.cs", 1, 1)
			}
		};

		VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void CacheItemProviderKeyFieldIsNotStatic()
	{
		var test = @"class Test {CacheItemProviderKey prop;}";

		var rule = new CacheItemProviderKeyMustBeStaticRule();
		var expected = new DiagnosticResult
		{
			Id = rule.DiagnosticDescriptor.Id,
			Message = rule.DiagnosticDescriptor.MessageFormat.ToString(),
			Severity = DiagnosticSeverity.Warning,
			Locations = new[]
			{
				new DiagnosticResultLocation("Test0.cs", 1, 13)
			}
		};

		VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void CacheItemProviderKeyPropertyIsNotStatic()
	{
		var test = @"class Test {CacheItemProviderKey prop => null;}";

		var rule = new CacheItemProviderKeyMustBeStaticRule();
		var expected = new DiagnosticResult
		{
			Id = rule.DiagnosticDescriptor.Id,
			Message = rule.DiagnosticDescriptor.MessageFormat.ToString(),
			Severity = DiagnosticSeverity.Warning,
			Locations = new[]
			{
				new DiagnosticResultLocation("Test0.cs", 1, 13)
			}
		};

		VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void CacheItemProviderKeyFieldAndPropertyIsStatic()
	{
		var test = @"class Test {static CacheItemProviderKey prop => null;static CacheItemProviderKey prop1;}";

		VerifyCSharpDiagnostic(test, Array.Empty<DiagnosticResult>());
	}

	[TestMethod]
	public void NotCacheItemProviderKeyFieldAndProperty()
	{
		var test = @"class Test {ItemProviderKey prop => null;ItemProviderKey prop1;}";

		VerifyCSharpDiagnostic(test, Array.Empty<DiagnosticResult>());
	}

	[TestMethod]
	public void ColumnRule_WorkingOnEfColumn()
	{
		var test =
			@"
			    class Test
			    {			    	
			    	[Column(""Id"")]
			    	public int TestProp {get;set;}
			    }";

		var rule = new NameOfInColumnAttributesRequiredRule();
		var expected = new DiagnosticResult
		{
			Id = rule.DiagnosticDescriptor.Id,
			Message = rule.DiagnosticDescriptor.MessageFormat.ToString(),
			Severity = DiagnosticSeverity.Warning,
			Locations = new[]
			{
				new DiagnosticResultLocation("Test0.cs", 4, 17)
			}
		};

		VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void ColumnRule_WorkingOnEfColumn_WithExplicitNameToken()
	{
		var test =
			@"
			    class Test
			    {			    	
			    	[Column(name:""Id"")]
			    	public int TestProp {get;set;}
			    }";

		var rule = new NameOfInColumnAttributesRequiredRule();
		var expected = new DiagnosticResult
		{
			Id = rule.DiagnosticDescriptor.Id,
			Message = rule.DiagnosticDescriptor.MessageFormat.ToString(),
			Severity = DiagnosticSeverity.Warning,
			Locations = new[]
			{
				new DiagnosticResultLocation("Test0.cs", 4, 22)
			}
		};

		VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void ColumnRule_WorkingForLinq2Sql()
	{
		var test =
			@"
			    class Test
			    {			    	
			    	[Column(Storage = ""Id"")]
			    	public int TestProp {get;set;}
			    }";

		var rule = new NameOfInColumnAttributesRequiredRule();
		var expected = new DiagnosticResult
		{
			Id = rule.DiagnosticDescriptor.Id,
			Message = rule.DiagnosticDescriptor.MessageFormat.ToString(),
			Severity = DiagnosticSeverity.Warning,
			Locations = new[]
			{
				new DiagnosticResultLocation("Test0.cs", 4, 27)
			}
		};

		VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void NoTestsWithoutOwnerRule()
	{
		var test =
			@"class TestBase{}

			    class Test1:TestBase
			    {
			    	[TestMethodAttribute]
			    	[OwnerAttribute(111)]
			    	public void TestMethod(){}

					[TestMethodAttribute]
					public void TestMethod2(){}

					public void NonTestMethod(){}
			    }";

		var rule = new NoTestWithoutOwnerRule();
		var expected = new DiagnosticResult
		{
			Id = rule.DiagnosticDescriptor.Id,
			Message = rule.DiagnosticDescriptor.MessageFormat.ToString(),
			Severity = DiagnosticSeverity.Hidden,
			Locations = new[]
			{
				new DiagnosticResultLocation("Test0.cs", 8, 1)
			}
		};

		VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void DataContractRule_DataContractAttributeOmitted_DiagnosticResult()
	{
		var test =
			@"
				using System.Runtime.Serialization;

				public class Test
				{
					[DataMember]
					public string Property { get; set; }
				}";
		var rule = new DataContractRequireIfUsingDataMemberRule();
		var expected = new DiagnosticResult()
		{
			Id = rule.DiagnosticDescriptor.Id,
			Message = rule.DiagnosticDescriptor.MessageFormat.ToString(),
			Severity = DiagnosticSeverity.Warning,
			Locations = new[]
			{
				new DiagnosticResultLocation("Test0.cs", 4, 18)
			}
		};

		VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void DataContractRule_DataContractAttributeOmittedWithField_DiagnosticResult()
	{
		var test =
			@"
				using System.Runtime.Serialization;

				public class Test
				{
					[DataMember]
					private string _field;
				}";
		var rule = new DataContractRequireIfUsingDataMemberRule();
		var expected = new DiagnosticResult()
		{
			Id = rule.DiagnosticDescriptor.Id,
			Message = rule.DiagnosticDescriptor.MessageFormat.ToString(),
			Severity = DiagnosticSeverity.Warning,
			Locations = new[]
			{
				new DiagnosticResultLocation("Test0.cs", 4, 18)
			}
		};

		VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void DataContractRule_DataContractAttributeProvided_NoDiagnostic()
	{
		var test =
			@"
				using System.Runtime.Serialization;

				[DataContract]
				public class Test
				{
					[DataMember]
					public string Property { get; set; }

					public string Property2 { get; set; }

					private string _field;
				}";

		VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void DataContractRule_DataContractAttributeProvidedOnBaseType_NoDiagnostic()
	{
		var test =
			@"
				using System.Runtime.Serialization;

				public class Child : Base
				{
					[DataMember]
					public string Property2 { get; set; }
				}

				[DataContract]
				public class Base
				{
					[DataMember]
					public string Property1 { get; set; }
				}";

		VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void DataContractRule_DataMemberAttributeOnBothParentAndChild_TwoDiagnosticResults()
	{
		var test =
			@"
				using System.Runtime.Serialization;

				public class Child : Base
				{
					[DataMember]
					public string Property2 { get; set; }
				}

				public class Base
				{
					[DataMember]
					public string Property1 { get; set; }
				}";
		var rule = new DataContractRequireIfUsingDataMemberRule();
		var expected = new[]
		{
			new DiagnosticResult()
			{
				Id = rule.DiagnosticDescriptor.Id,
				Message = rule.DiagnosticDescriptor.MessageFormat.ToString(),
				Severity = DiagnosticSeverity.Warning,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 4, 18)
				}
			},
			new DiagnosticResult()
			{
				Id = rule.DiagnosticDescriptor.Id,
				Message = rule.DiagnosticDescriptor.MessageFormat.ToString(),
				Severity = DiagnosticSeverity.Warning,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 10, 18)
				}
			},
		};

		VerifyCSharpDiagnostic(test, expected);
	}

	/*
	[TestMethod]
	public void TabsFormattedBySpaces()
	{
		var test = @"      var nikita = 1;";
		var expected = new DiagnosticResult
		{
			Id = MindboxAnalyzer.OnlyTabsShouldBeUsedForIndentationRuleId,
			Message = "Для отступа используются пробелы",
			Severity = DiagnosticSeverity.Warning,
			Locations = new[]
			{
				new DiagnosticResultLocation("Test0.cs", 1, 1)
			}
		};

		VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void RegionInsideAMethod()
	{
		var test = @"public void Main() { #region var a = 5; #endregion var b = 3; return; }";
		var expected = new DiagnosticResult
		{
			Id = MindboxAnalyzer.NoRegionsInsideMethodsRuleId,
			Message = "Использование региона внутри метода",
			Severity = DiagnosticSeverity.Warning,
			Locations = new[]
			{
				new DiagnosticResultLocation("Test0.cs", 1, 22)
			}
		};

		VerifyCSharpDiagnostic(test, expected);
	}
	*/
	protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
	{
		return new MindboxAnalyzer();
	}
}