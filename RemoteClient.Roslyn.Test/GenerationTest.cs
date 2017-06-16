using System;
using Xunit;

namespace RemoteClient.Roslyn.Test
{
    public class GenerationTest
    {
        [Fact]
        public void Test1()
        {
			var foo = new Foo();
	        var fooa = new FooA();
			Assert.Equal("100", fooa.Value);
        }
    }

	[DuplicateWithSuffix("A")]
	public class Foo
	{
		public string Value { get; } = "100";
	}
;}
