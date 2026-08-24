"use client";

import { useEffect, useState, useRef } from "react";
import type { BannerSectionItem } from "@/modules/banners/types";
import { ResponsiveBannerSlideView, type ResponsiveBannerSlide } from "./banner-sections";

export function HeroBannerCarousel({
  slides,
  items,
  variant = "desktop",
}: {
  slides?: ResponsiveBannerSlide[];
  items?: BannerSectionItem[];
  variant?: "desktop" | "mobile";
}) {
  const effectiveSlides: ResponsiveBannerSlide[] =
    slides && slides.length > 0
      ? slides
      : (items || []).map((item, idx) => ({
          id: item.id || `slide-${idx}`,
          desktopItem: item,
          mobileItem: item,
        }));

  const [currentIndex, setCurrentIndex] = useState(0);
  const containerRef = useRef<HTMLDivElement>(null);
  const scrollTimeout = useRef<NodeJS.Timeout>(null);

  const isDragging = useRef(false);
  const startX = useRef(0);
  const scrollLeft = useRef(0);
  const dragged = useRef(false);

  useEffect(() => {
    if (effectiveSlides.length <= 1) return;
    const interval = setInterval(() => {
      if (!isDragging.current) {
        setCurrentIndex((prev) => (prev + 1) % effectiveSlides.length);
      }
    }, 5000);
    return () => clearInterval(interval);
  }, [effectiveSlides.length]);

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

  if (effectiveSlides.length === 0) return null;

  return (
    <div className="relative w-full aspect-square md:aspect-auto md:h-[75vh] overflow-hidden group">
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
        {effectiveSlides.map((slide, index) => (
          <div key={slide.id} className="w-full h-full flex-shrink-0 snap-start">
            <ResponsiveBannerSlideView
              slide={slide}
              priority={index === 0}
            />
          </div>
        ))}
      </div>

      {effectiveSlides.length > 1 && (
        <div className="absolute bottom-4 sm:bottom-5 left-0 right-0 flex justify-center gap-2 sm:gap-3 z-10">
          {effectiveSlides.map((_, index) => (
            <button
              key={index}
              onClick={() => goToSlide(index)}
              className={`size-2.5 sm:size-3 rounded-full cursor-pointer transition-colors ${
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
