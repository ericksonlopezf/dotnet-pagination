// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Pagination.MongoDB;

internal sealed class KeysetMongoProjection<TItem, TKey>
{
    public TItem Item { get; set; } = default!;
    public TKey Key { get; set; } = default!;
}
