"use server";

export async function fetchProvinces() {
  try {
    const res = await fetch("https://turkiyeapi.dev/api/v1/provinces", { 
      next: { revalidate: 86400 },
      headers: { "Accept": "application/json" }
    });
    if (!res.ok) throw new Error("Failed to fetch provinces");
    return await res.json();
  } catch (error) {
    console.error("TurkiyeAPI Provinces Error:", error);
    return { data: [] };
  }
}

export async function fetchNeighborhoods(districtId: number) {
  try {
    const res = await fetch(`https://turkiyeapi.dev/api/v1/neighborhoods?districtId=${districtId}`, { 
      next: { revalidate: 86400 },
      headers: { "Accept": "application/json" }
    });
    if (!res.ok) throw new Error("Failed to fetch neighborhoods");
    return await res.json();
  } catch (error) {
    console.error("TurkiyeAPI Neighborhoods Error:", error);
    return { data: [] };
  }
}
