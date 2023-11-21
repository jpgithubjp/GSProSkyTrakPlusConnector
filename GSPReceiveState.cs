using System.Text;

namespace SkytrakOpenAPI
{
    public class GSPReceiveState
    {
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[1024];
        public StringBuilder sb = new();
    }
}
