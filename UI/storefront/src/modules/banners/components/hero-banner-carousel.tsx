"use client";

import { useEffect, useState, useRef } from "react";
import type { BannerSectionItem } from "@/modules/banners/types";
import { BannerMedia } from "./banner-sections";

export function HeroBannerCarousel({
  items,
  variant = "desktop",
}: {
  items: BannerSectionItem[];
  variant?: "desktop" | "mobile";
}) {
  const [currentIndex, setCurrentIndex] = useState(0);
  const containerRef = useRef<HTMLDivElement>(null);
  const scrollTimeout = useRef<NodeJS.Timeout>(null);

  const isDragging = useRef(false);
  const startX = useRef(0);
  const scrollLeft = useRef(0);
  const dragged = useRef(false);

  useEffect(() => {
    if (items.length <= 1) return;
    const interval = setInterval(() => {
      if (!isDragging.current) {
        setCurrentIndex((prev) => (prev + 1) % items.length);
      }
    }, 5000);
    return () => clearInterval(interval);
  }, [items.length]);

  useEffect(() => {
    if (containerRef.current) {
      const container = containerRef.current;
      container.scrollTo({
        left: container.clientWidth * currentIndex,
        behavior: "smooth",
      });
    }
  }, [currentIndex]);

  const goToSlide = (index: number) => {
    setCurrentIndex(index);
  };

  const handleScroll = () => {
    if (scrollTimeout.current) clearTimeout(scrollTimeout.current);
    scrollTimeout.current = setTimeout(() => {
      if (containerRef.current && !isDragging.current) {
        const index = Math.round(
          containerRef.current.scrollLeft / containerRef.current.clientWidth,
        );
        if (index !== currentIndex) {
          setCurrentIndex(index);
        }
      }
    }, 150);
  };

  const onMouseDown = (e: React.MouseEvent) => {
    isDragging.current = true;
    dragged.current = false;
    startX.current = e.pageX - containerRef.current!.offsetLeft;
    scrollLeft.current = containerRef.current!.scrollLeft;
    if (containerRef.current) {
      containerRef.current.style.scrollBehavior = "auto";
      containerRef.current.style.scrollSnapType = "none";
    }
  };

  const onMouseLeave = () => {
    if (isDragging.current) {
      isDragging.current = false;
      snapToClosest();
    }
  };

  const onMouseUp = () => {
    if (isDragging.current) {
      isDragging.current = false;
      snapToClosest();
    }
  };

  const snapToClosest = () => {
    if (containerRef.current) {
      const index = Math.round(
        containerRef.current.scrollLeft / containerRef.current.clientWidth,
      );
      setCurrentIndex(index);
      containerRef.current.style.scrollBehavior = "smooth";
      containerRef.current.style.scrollSnapType = "x mandatory";
      containerRef.current.scrollTo({
        left: containerRef.current.clientWidth * index,
        behavior: "smooth",
      });
    }
  };

  const onMouseMove = (e: React.MouseEvent) => {
    if (!isDragging.current || !containerRef.current) return;
    e.preventDefault();
    const x = e.pageX - containerRef.current.offsetLeft;
    const walk = x - startX.current;
    if (Math.abs(walk) > 5) dragged.current = true;
    containerRef.current.scrollLeft = scrollLeft.current - walk;
  };

  const onClickCapture = (e: React.MouseEvent) => {
    if (dragged.current) {
      e.stopPropagation();
      e.preventDefault();
    }
  };

  const onDragStart = (e: React.DragEvent) => {
    e.preventDefault();
  };

  const heightClass = variant === "mobile" ? "aspect-square w-full" : "h-[75vh] w-full";

  return (
    <div className={`relative w-full ${heightClass} overflow-hidden group`}>
      <div
        ref={containerRef}
        onScroll={handleScroll}
        onMouseDown={onMouseDown}
        onMouseLeave={onMouseLeave}
        onMouseUp={onMouseUp}
        onMouseMove={onMouseMove}
        onClickCapture={onClickCapture}
        onDragStart={onDragStart}
        className="flex h-full w-full overflow-x-auto overflow-y-hidden snap-x snap-mandatory scroll-smooth"
        style={{ scrollbarWidth: "none", msOverflowStyle: "none" }}
      >
        {items.map((item, index) => (
          <div key={item.id} className="w-full h-full flex-shrink-0 snap-start">
            <BannerMedia
              item={item}
              priority={index === 0}
              variant={variant === "mobile" ? "mobile-main" : "main"}
            />
          </div>
        ))}
      </div>

      {items.length > 1 && (
        <div className="absolute bottom-4 sm:bottom-5 left-0 right-0 flex justify-center gap-2 sm:gap-3 z-10">
          {items.map((_, index) => (
            <button
              key={index}
              onClick={() => goToSlide(index)}
              className={`size-2.5 sm:size-3 rounded-full transition-colors ${
                index === currentIndex ? "bg-zinc-950" : "bg-zinc-400 hover:bg-zinc-600"
              }`}
              aria-label={`Slayt ${index + 1}'e git`}
            />
          ))}
        </div>
      )}
    </div>
  );
}
