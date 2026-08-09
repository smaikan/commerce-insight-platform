// Burada OpenAPI'nin bazı liste response'larında eksik bıraktığı belgeli ortak sayfalama gövdesini tek yerde tutuyorum.
export type PagedResult<T> = {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};
