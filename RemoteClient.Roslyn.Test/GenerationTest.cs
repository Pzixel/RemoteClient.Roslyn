using System;
using Xunit;

namespace RemoteClient.Roslyn.Test
{
    public class GenerationTest
    {
        [Fact]
        public void Test1()
        {
			var fooCLient = new IFooClient(null);
        }
    }

	[RemoteClient]
	public interface IFoo
	{
		
	}
;}
