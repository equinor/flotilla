import { AccessTokenContext } from 'components/FrontPage/FlotillaSite'
import { config } from 'config'
import { Robot } from 'models/robot'
import { ScheduledMission } from 'models/scheduledMission'
import { useContext } from 'react'

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

    async getRobots() {
        const path: string = 'robots'
        const result = await this.GET<Robot>(path).catch((e) => {
            throw new Error(`Failed to GET /${path}: ` + e)
        })
        return result
    }

    async getUpcomingMissions() {
        const path: string = 'scheduled-missions/upcoming'
        const result = await this.GET<ScheduledMission[]>(path).catch((e) => {
            throw new Error(`Failed to GET /${path}: ` + e)
        })
        return result
    }
}

export const useApi = () => {
    const accessToken = useContext(AccessTokenContext)
    return new BackendAPICaller(accessToken)
}
