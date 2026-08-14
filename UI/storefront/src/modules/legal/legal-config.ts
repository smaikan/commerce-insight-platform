import "server-only";

// Burada işletmeye özgü yasal kimlik alanlarını uydurmadan, yayın ortamından doldurulabilir tek yapılandırmada topluyorum.
export const legalConfig = {
  businessName: process.env.LEGAL_BUSINESS_NAME?.trim() || null,
  address: process.env.LEGAL_BUSINESS_ADDRESS?.trim() || null,
  email: process.env.LEGAL_CONTACT_EMAIL?.trim() || null,
  phone: process.env.LEGAL_CONTACT_PHONE?.trim() || null,
  mersisNumber: process.env.LEGAL_MERSIS_NUMBER?.trim() || null,
  taxOffice: process.env.LEGAL_TAX_OFFICE?.trim() || null,
  taxNumber: process.env.LEGAL_TAX_NUMBER?.trim() || null,
} as const;
