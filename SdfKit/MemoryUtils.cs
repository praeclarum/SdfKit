using System.Buffers;
using System.Runtime.InteropServices;

namespace SdfKit;

// From, https://stackoverflow.com/questions/54511330/how-can-i-cast-memoryt-to-another
static class MemoryUtils
{
    public static Memory<TTo> Cast<TFrom, TTo>(Memory<TFrom> from)
        where TFrom : unmanaged
        where TTo : unmanaged
    {
        // avoid the extra allocation/indirection, at the cost of a gen-0 box
        if (typeof(TFrom) == typeof(TTo)) return (Memory<TTo>)(object)from;

        return new CastMemoryManager<TFrom, TTo>(from).Memory;
    }
    private sealed class CastMemoryManager<TFrom, TTo> : MemoryManager<TTo>
        where TFrom : unmanaged
        where TTo : unmanaged
    {
        private readonly Memory<TFrom> _from;

        public CastMemoryManager(Memory<TFrom> from) => _from = from;

        public override Span<TTo> GetSpan()
            => MemoryMarshal.Cast<TFrom, TTo>(_from.Span);

        protected override void Dispose(bool disposing) { }
        public override MemoryHandle Pin(int elementIndex = 0)
            => _from.Pin();
        public override void Unpin()
            => throw new NotSupportedException();
    }
}
