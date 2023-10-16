using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace WeddingImageBoard.InfiniteScrolling
{
    public sealed class InfiniteScrollingItemsProviderRequest
    {
        public InfiniteScrollingItemsProviderRequest(int startIndex, CancellationToken cancellationToken)
        {
            StartIndex = startIndex;
            CancellationToken = cancellationToken;
        }

        public int StartIndex { get; }
        public CancellationToken CancellationToken { get; }
    }

    public delegate Task<IEnumerable<T>> ItemsProviderRequestDelegate<T>(InfiniteScrollingItemsProviderRequest request);

}
