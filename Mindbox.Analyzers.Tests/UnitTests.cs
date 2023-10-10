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
	public void ForbidMarsRule_Marked_Property_Test()
	{
		var test = @"
class PropertyOwner
{
	[LastResortUse]
	public bool TestProperty {get;set;}
}

class TestClass
{
	public TestClass()
	{
		var po = new PropertyOwner();
		po.TestProperty = true;
	}
}
		";
		var rule = new AvoidUsingMarkedWithLastResortUse();
		var expected = new DiagnosticResult
		{
			Id = rule.DiagnosticDescriptor.Id,
			Message = rule.DiagnosticDescriptor.MessageFormat.ToString(),
			Severity = DiagnosticSeverity.Error,
			Locations = new[]
			{
				new DiagnosticResultLocation("Test0.cs", 13, 3)
			}
		};
		VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void ForbidMarsRule_Marked_PropertyOwner_Test()
	{
		var test = @"
[LastResortUse]
class PropertyOwner
{
	public bool TestProperty {get;set;}
}

class TestClass
{
	public TestClass()
	{
		var po = new PropertyOwner();
		po.TestProperty = true;
	}
}
		";
		var rule = new AvoidUsingMarkedWithLastResortUse();
		var expected = new DiagnosticResult
		{
			Id = rule.DiagnosticDescriptor.Id,
			Message = rule.DiagnosticDescriptor.MessageFormat.ToString(),
			Severity = DiagnosticSeverity.Error,
			Locations = new[]
			{
				new DiagnosticResultLocation("Test0.cs", 13, 3)
			}
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