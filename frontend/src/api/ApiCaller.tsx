import { AccessTokenContext } from 'components/Pages/FlotillaSite'
import { config } from 'config'
import { Mission } from 'models/Mission'
import { Robot } from 'models/Robot'
import { useContext, useEffect, useRef } from 'react'

export class BackendAPICaller {
    /* Implements the request sent to the backend api.
     */
    accessToken: string

    constructor(accessToken: string) {
        this.accessToken = accessToken
    }

    private async query<T>(
        method: 'GET' | 'POST' | 'PUT' | 'DELETE',
        path: string,
        body?: T
    ): Promise<{ body: T; headers: Headers }> {
        const headers = {
            'content-type': 'application/json',
            Authorization: `Bearer ${this.accessToken}`,
        }

        const init: RequestInit = {
            method,
            headers,
            mode: 'cors',
        }
        if (body !== undefined) {
            init.body = JSON.stringify(body)
        }

        const url = `${config.BACKEND_URL}/${path}`

        const response: Response = await fetch(url, init)
        if (!response.ok) throw new Error(`${response.status} - ${response.statusText}`)
        const responseBody = await response.json().catch((e) => {
            throw new Error(`Error getting json from response: ${e}`)
        })
        return { body: responseBody, headers: response.headers }
    }

    private async GET<T>(path: string): Promise<{ body: T; headers: Headers }> {
        return this.query('GET', path)
    }

    private async POST<T>(path: string, body: T): Promise<{ body: T; headers: Headers }> {
        return this.query('POST', path, body)
    }

    private async PUT<T>(path: string, body: T): Promise<{ body: T; headers: Headers }> {
        return this.query('PUT', path, body)
    }

    private async DELETE<T>(path: string, body: T): Promise<{ body: T; headers: Headers }> {
        return this.query('DELETE', path, body)
    }

    async getRobots(): Promise<Robot[]> {
        const path: string = 'robots'
        const result = await this.GET<Robot[]>(path).catch((e) => {
            throw new Error(`Failed to GET /${path}: ` + e)
        })
        return result.body
    }

    async getUpcomingMissions() {
        const path: string = 'scheduled-missions/upcoming'
        const result = await this.GET<Mission[]>(path).catch((e) => {
            throw new Error(`Failed to GET /${path}: ` + e)
        })
        return result
    }
}

export const useApi = () => {
    const accessToken = useContext(AccessTokenContext)
    return new BackendAPICaller(accessToken)
}

export function useInterval(callbackFunction: () => void) {
    // Used to call a function at a fixed intervall
    const delay = 5000
    const savedCallback = useRef<() => void>(Function)
    // Remember the latest callback function
    useEffect(() => {
        savedCallback.current = callbackFunction
    }, [callbackFunction])

    useEffect(() => {
        function tick() {
            savedCallback.current()
        }
        if (delay != null) {
            const id = setInterval(tick, delay)
            return () => {
                clearInterval(id)
            }
        }
    }, [callbackFunction, delay])
}
