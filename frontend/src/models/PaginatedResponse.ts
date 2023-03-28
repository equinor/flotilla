export interface PaginationHeader {
    TotalCount: number
    PageSize: number
    CurrentPage: number
    TotalPages: number
    HasNext: boolean
    HasPrevious: boolean
}

export interface PaginatedResponse<T> {
    pagination: PaginationHeader
    content: T[]
}

export const PaginationHeaderName = 'X-Pagination'
