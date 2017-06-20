using System;
using System.Threading.Tasks;
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

	[RemoteClient(true)]
	public interface IFoo
	{
		Task<string> GetStringAsync(string value);
	}



	[RemoteClient]
	public interface IBar
	{

	}
;
}
