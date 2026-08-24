"use client";

import { useState } from "react";
import { StockMovementForm } from "@/modules/inventory/components/stock-movement-form";
import { ProductForm } from "@/modules/products/components/product-form";
import type { Product, ProductFormOptions, ProductImage } from "@/modules/products/types";
import { editableVariantRevision } from "@/modules/products/variant-editing";

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

  return (
    <>
      <ProductForm
        key={`${product.id}:${product.status}:${product.hasVariants}:${product.mainSku}:${editableVariantRevision(product.variants)}`}
        mode="edit"
        product={product}
        images={images}
        options={options}
        onDirtyChange={setProductFormDirty}
      />
      <StockMovementForm
        productId={product.id}
        variantOptions={product.variants}
        disabled={productFormDirty}
      />
    </>
  );
}
