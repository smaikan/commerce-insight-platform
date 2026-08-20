"use client";

import { useEffect, useState, type SelectHTMLAttributes, useTransition } from "react";
import { fetchProvinces, fetchNeighborhoods } from "./turkiye-address-actions";

interface Province { id: number; name: string; districts?: District[]; }
interface District { id: number; name: string; }
interface Neighborhood { id: number; name: string; }

function sortProvinces(provinces: Province[]) {
  const topIds = [34, 6, 35]; // İstanbul, Ankara, İzmir
  const top = provinces.filter(p => topIds.includes(p.id)).sort((a, b) => topIds.indexOf(a.id) - topIds.indexOf(b.id));
  const rest = provinces.filter(p => !topIds.includes(p.id)).sort((a, b) => a.name.localeCompare(b.name, "tr"));
  return [...top, ...rest];
}

export function TurkiyeAddressFields({
  prefix = "",
  errors = {},
  defaultCity = "",
  defaultDistrict = "",
  defaultNeighborhood = "",
  variant = "checkout",
}: {
  prefix?: string;
  errors?: Record<string, string>;
  defaultCity?: string;
  defaultDistrict?: string;
  defaultNeighborhood?: string;
  variant?: "checkout" | "account";
}) {
  const [provinces, setProvinces] = useState<Province[]>([]);
  const [districts, setDistricts] = useState<District[]>([]);
  const [neighborhoods, setNeighborhoods] = useState<Neighborhood[]>([]);
  
  const [selectedProvinceId, setSelectedProvinceId] = useState<number | "">("");
  const [selectedProvinceName, setSelectedProvinceName] = useState(defaultCity);

  const [selectedDistrictId, setSelectedDistrictId] = useState<number | "">("");
  const [selectedDistrictName, setSelectedDistrictName] = useState(defaultDistrict);

  const [selectedNeighborhoodName, setSelectedNeighborhoodName] = useState(defaultNeighborhood);

  const [isPending, startTransition] = useTransition();

  // 1. Fetch Provinces (v1 includes districts)
  useEffect(() => {
    startTransition(async () => {
      const res = await fetchProvinces();
      if (!res.data) return;
      const data = sortProvinces(res.data);
      setProvinces(data);
      
      if (defaultCity) {
        const found = data.find(p => p.name.toLocaleUpperCase("tr") === defaultCity.toLocaleUpperCase("tr"));
        if (found) {
          setSelectedProvinceId(found.id);
          setDistricts(found.districts || []);
          
          if (defaultDistrict) {
            const foundDist = (found.districts || []).find((d: District) => d.name.toLocaleUpperCase("tr") === defaultDistrict.toLocaleUpperCase("tr"));
            if (foundDist) setSelectedDistrictId(foundDist.id);
          }
        }
      }
    });
  }, [defaultCity, defaultDistrict]);

  // 2. Fetch Neighborhoods when District changes
  useEffect(() => {
    if (!selectedDistrictId) {
      setNeighborhoods([]);
      return;
    }
    startTransition(async () => {
      const res = await fetchNeighborhoods(selectedDistrictId as number);
      if (res.data) setNeighborhoods(res.data);
    });
  }, [selectedDistrictId]);

  function handleProvinceChange(e: React.ChangeEvent<HTMLSelectElement>) {
    const id = Number(e.target.value);
    setSelectedProvinceId(id || "");
    const name = e.target.options[e.target.selectedIndex]?.text || "";
    setSelectedProvinceName(id ? name : "");
    setSelectedDistrictId("");
    setSelectedDistrictName("");
    setSelectedNeighborhoodName("");
    
    if (id) {
      const found = provinces.find(p => p.id === id);
      setDistricts(found?.districts || []);
    } else {
      setDistricts([]);
    }
    setNeighborhoods([]);
  }

  function handleDistrictChange(e: React.ChangeEvent<HTMLSelectElement>) {
    const id = Number(e.target.value);
    setSelectedDistrictId(id || "");
    const name = e.target.options[e.target.selectedIndex]?.text || "";
    setSelectedDistrictName(id ? name : "");
    setSelectedNeighborhoodName("");
  }

  function handleNeighborhoodChange(e: React.ChangeEvent<HTMLSelectElement>) {
    setSelectedNeighborhoodName(e.target.value);
  }

  const labelClass = variant === "checkout" 
    ? "mb-2 block text-sm font-semibold text-ink" 
    : "block text-xs font-bold text-ink";
    
  const inputClass = variant === "checkout"
    ? "focus-ring min-h-12 w-full rounded-lg border border-line bg-surface px-3 text-sm text-ink aria-[invalid=true]:border-danger disabled:bg-surface-subtle disabled:text-ink-muted"
    : "focus-ring mt-2 min-h-11 w-full border border-line bg-surface px-3 text-sm font-normal text-ink disabled:bg-surface-subtle disabled:text-ink-muted";

  return (
    <>
      <input type="hidden" name={`${prefix}City`} value={selectedProvinceName} />
      <input type="hidden" name={`${prefix}District`} value={selectedDistrictName} />
      <input type="hidden" name={`${prefix}Neighborhood`} value={selectedNeighborhoodName} />

      <SelectField label="İl" error={errors[`${prefix}City`]} value={selectedProvinceId} onChange={handleProvinceChange} labelClass={labelClass} inputClass={inputClass}>
        <option value="">İl seçin</option>
        {provinces.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
      </SelectField>

      <SelectField label="İlçe" error={errors[`${prefix}District`]} value={selectedDistrictId} onChange={handleDistrictChange} disabled={!selectedProvinceId} labelClass={labelClass} inputClass={inputClass} required={true}>
        <option value="">İlçe seçin</option>
        {districts.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
      </SelectField>

      <SelectField label="Mahalle" error={errors[`${prefix}Neighborhood`]} value={selectedNeighborhoodName} onChange={handleNeighborhoodChange} disabled={!selectedDistrictId} labelClass={labelClass} inputClass={inputClass} required={false}>
        <option value="">Mahalle seçin</option>
        {neighborhoods.map(n => <option key={n.id} value={n.name}>{n.name}</option>)}
      </SelectField>
    </>
  );
}

function SelectField({ label, error, labelClass, inputClass, children, required = true, ...props }: SelectHTMLAttributes<HTMLSelectElement> & { label: string; error?: string; labelClass: string; inputClass: string; required?: boolean }) {
  return (
    <label className={variantWrapper(labelClass)}>
      {variantWrapperInner(labelClass, label, required)}
      <select {...props} aria-invalid={Boolean(error)} className={inputClass} required={required}>
        {children}
      </select>
      {error ? <span className="mt-1.5 block text-sm font-semibold text-danger">{error}</span> : null}
    </label>
  );
}

function variantWrapper(labelClass: string) {
  if (labelClass.includes("mb-2")) return "block";
  return labelClass;
}

function variantWrapperInner(labelClass: string, label: string, required: boolean) {
  if (labelClass.includes("mb-2")) {
    return <span className={labelClass}>{label}{required ? <span className="ml-1 text-danger" aria-hidden="true">*</span> : null}</span>;
  }
  return <>{label}</>;
}
