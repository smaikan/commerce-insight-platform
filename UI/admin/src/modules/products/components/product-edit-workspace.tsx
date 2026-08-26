"use client";

import { useState } from "react";
import { StockMovementForm } from "@/modules/inventory/components/stock-movement-form";
import { ProductForm } from "@/modules/products/components/product-form";
import type { Product, ProductFormOptions, ProductImage } from "@/modules/products/types";
import { productFormIdentity, reconcileProductFormIdentity } from "@/modules/products/product-edit-revision";

// Burada ürün kaydıyla stok hareketini ayrı formlar olarak tutarken kaydedilmemiş ürün taslağının üzerine stok yenilemesi yazılmasını engelliyorum.
export function ProductEditWorkspace({
  product,
  images,
  options,
}: {
  product: Product;
  images: ProductImage[];
  options: ProductFormOptions;
}) {
  const [productFormDirty, setProductFormDirty] = useState(false);
  const incomingProductFormIdentity = productFormIdentity(product);
  const [draftProductFormIdentity, setDraftProductFormIdentity] = useState(incomingProductFormIdentity);
  const activeProductFormIdentity = reconcileProductFormIdentity(
    draftProductFormIdentity,
    incomingProductFormIdentity,
    productFormDirty,
  );

  // Burada ilk taslak değişikliğinde mevcut sunucu revizyonunu dondurup medya kaydı tamamlanana kadar form kimliğini koruyorum.
  function handleProductFormDirtyChange(nextDirty: boolean) {
    if (nextDirty && !productFormDirty) setDraftProductFormIdentity(incomingProductFormIdentity);
    setProductFormDirty(nextDirty);
  }

  return (
    <>
      <ProductForm
        key={`${activeProductFormIdentity.productId}:${activeProductFormIdentity.revision}`}
        mode="edit"
        product={product}
        images={images}
        options={options}
        onDirtyChange={handleProductFormDirtyChange}
      />
      <StockMovementForm
        productId={product.id}
        variantOptions={product.variants}
        disabled={productFormDirty}
      />
    </>
  );
}
