import { config } from 'config'
import { ApiError } from './ApiError'
/** Implements the request sent to the backend api. */

type GetAccessToken = () => Promise<string>

export class BackendAPICaller {
    private readonly getAccessToken: GetAccessToken

    constructor(getAccessToken: GetAccessToken) {
        this.getAccessToken = getAccessToken
    }

    private async initializeRequest<T>(
        method: 'GET' | 'POST' | 'PUT' | 'DELETE',
        body?: T,
        contentType?: string,
        opts?: { headers?: Record<string, string>; signal?: AbortSignal }
    ): Promise<RequestInit> {
        const token = await this.getAccessToken()

        const headers: Record<string, string> = {
            Authorization: `Bearer ${token}`,
            ...(opts?.headers ?? {}),
        }

        if (contentType) headers['content-type'] = contentType
        else if (body !== undefined) headers['content-type'] = 'application/json'

        const init: RequestInit = {
            method,
            headers,
            mode: 'cors',
            signal: opts?.signal,
        }

        if (body !== undefined) init.body = JSON.stringify(body)
        return init
    }

    private async query<TBody, TContent>(
        method: 'GET' | 'POST' | 'PUT' | 'DELETE',
        path: string,
        body?: TBody,
        contentType?: string,
        opts?: { headers?: Record<string, string>; signal?: AbortSignal }
    ): Promise<{ content: TContent; headers: Headers }> {
        const url = `${config.BACKEND_URL}/${path}`

        let init = await this.initializeRequest(method, body, contentType, opts)
        let response = await fetch(url, init)

        // Retry once on 401 with a freshly acquired token (new headers)
        if (response.status === 401) {
            init = await this.initializeRequest(method, body, contentType, opts)
            response = await fetch(url, init)
        }

        if (!response.ok) throw ApiError.fromCode(response.status, response.statusText, await response.text())

        let responseContent: any = ''
        // Status code 204 means no content
        if (response.status !== 204) {
            if (contentType === 'image/png') {
                responseContent = await response.blob().catch((e) => {
                    throw new Error(`Error getting blob from response: ${e}`)
                })
            } else {
                responseContent = await response.json().catch((e) => {
                    throw new Error(`Error getting json from response: ${e}`)
                })
            }
        }
        return { content: responseContent as TContent, headers: response.headers }
    }

    GET<TContent>(
        path: string,
        contentType?: string,
        opts?: { headers?: Record<string, string>; signal?: AbortSignal }
    ) {
        return this.query<never, TContent>('GET', path, undefined, contentType, opts)
    }

    POST<TBody, TContent>(
        path: string,
        body: TBody,
        contentType?: string,
        opts?: { headers?: Record<string, string>; signal?: AbortSignal }
    ) {
        return this.query<TBody, TContent>('POST', path, body, contentType, opts)
    }

    PUT<TBody, TContent>(
        path: string,
        body?: TBody,
        contentType?: string,
        opts?: { headers?: Record<string, string>; signal?: AbortSignal }
    ) {
        return this.query<TBody, TContent>('PUT', path, body, contentType, opts)
    }

    DELETE<TBody, TContent>(
        path: string,
        body?: TBody,
        contentType?: string,
        opts?: { headers?: Record<string, string>; signal?: AbortSignal }
    ) {
        return this.query<TBody, TContent>('DELETE', path, body, contentType, opts)
    }

    GET_BLOB(path: string, opts?: { headers?: Record<string, string>; signal?: AbortSignal }) {
        return this.queryBlob(path, opts)
    }

    private async queryBlob(
        path: string,
        opts?: { headers?: Record<string, string>; signal?: AbortSignal }
    ): Promise<{ content: Blob; headers: Headers }> {
        const url = `${config.BACKEND_URL}/${path}`

        let init = await this.initializeRequest('GET', undefined, undefined, opts)
        let response = await fetch(url, init)

        // Retry once on 401 with fresh headers/token
        if (response.status === 401) {
            init = await this.initializeRequest('GET', undefined, undefined, opts)
            response = await fetch(url, init)
        }

        if (!response.ok) {
            throw ApiError.fromCode(response.status, response.statusText, await response.text())
        }

        return { content: await response.blob(), headers: response.headers }
    }
}
