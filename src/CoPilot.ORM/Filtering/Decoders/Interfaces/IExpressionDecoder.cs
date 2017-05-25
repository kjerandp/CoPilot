using System.Reflection;

namespace CoPilot.ORM.Filtering.Decoders.Interfaces
{
    public interface IExpressionDecoder
    {
        IDecodedNode Decode();
    }
}
