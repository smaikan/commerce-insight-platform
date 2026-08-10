using ECommerce.Domain.Entities;
using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Common.Interfaces;

public interface IProductImageRepository
{
    // Burada yeni ürün görselini kalıcı depoya ekleme sözleşmesini tanımlıyorum.
    Task AddAsync(ProductImage image, CancellationToken cancellationToken = default);
    // Burada takip edilen ürün görselini kalıcı depodan silme sözleşmesini tanımlıyorum.
    void Remove(ProductImage image);
    // Burada ürün görselini katalog okuması için getirme sözleşmesini tanımlıyorum.
    Task<ProductImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada aktif katalogdaki ürün görselini güncelleme için getirme sözleşmesini tanımlıyorum.
    Task<ProductImage?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada silinmiş ürünün görselleri dahil görseli bağımsız silme için getirme sözleşmesini tanımlıyorum.
    Task<ProductImage?> GetByIdForDeletionAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada ürünün mevcut ana görselini güncelleme için getirme sözleşmesini tanımlıyorum.
    Task<ProductImage?> GetMainByProductIdForUpdateAsync(
        long productId,
        Guid? excludedImageId = null,
        CancellationToken cancellationToken = default);
    // Burada ürüne ait görsel sayısını okuma sözleşmesini tanımlıyorum.
    Task<int> CountByProductIdAsync(long productId, CancellationToken cancellationToken = default);
    // Burada ana görsel yerine geçirilecek ilk görseli getirme sözleşmesini tanımlıyorum.
    Task<ProductImage?> GetFirstByProductIdForUpdateAsync(
        long productId,
        Guid excludedImageId,
        CancellationToken cancellationToken = default);
    // Burada ürünün görsellerini sayfalı okuma sözleşmesini tanımlıyorum.
    Task<PagedResult<ProductImage>> GetByProductIdAsync(
        long productId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
