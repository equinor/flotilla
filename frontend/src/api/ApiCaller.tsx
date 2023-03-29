import { AccessTokenContext } from 'components/Pages/FlotillaSite'
import { config } from 'config'
import { EchoMission, EchoPlantInfo } from 'models/EchoMission'
import { Mission, MissionStatus } from 'models/Mission'
import { Robot } from 'models/Robot'
import { VideoStream } from 'models/VideoStream'
import { useContext } from 'react'
import { filterRobots } from 'utils/filtersAndSorts'
import { MissionQueryParameters } from 'models/MissionQueryParameters'
import { PaginatedResponse, PaginationHeader, PaginationHeaderName } from 'models/PaginatedResponse'

export class BackendAPICaller {
    /* Implements the request sent to the backend api.
     */
    accessToken: string

    constructor(accessToken: string) {
        this.accessToken = accessToken
    }

    private initializeRequest<T>(method: 'GET' | 'POST' | 'PUT' | 'DELETE', body?: T): RequestInit {
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
        return init
    }

    private async query<TBody, TContent>(
        method: 'GET' | 'POST' | 'PUT' | 'DELETE',
        path: string,
        body?: TBody
    ): Promise<{ content: TContent; headers: Headers }> {
        const initializedRequest: RequestInit = this.initializeRequest(method, body)

        const url = `${config.BACKEND_URL}/${path}`
        const response: Response = await fetch(url, initializedRequest)
        if (!response.ok) throw new Error(`${response.status} - ${response.statusText}`)
        var responseContent
        // Status code 204 means no content
        if (response.status !== 204) {
            responseContent = await response.json().catch((e) => {
                throw new Error(`Error getting json from response: ${e}`)
            })
        } else responseContent = ''
        return { content: responseContent, headers: response.headers }
    }

    private async GET<TContent>(path: string): Promise<{ content: TContent; headers: Headers }> {
        return this.query('GET', path)
    }

    private async POST<TBody, TContent>(path: string, body: TBody): Promise<{ content: TContent; headers: Headers }> {
        return this.query('POST', path, body)
    }

    private async DELETE<TBody, TContent>(path: string, body: TBody): Promise<{ content: TContent; headers: Headers }> {
        return this.query('DELETE', path, body)
    }

    private async postControlMissionRequest(path: string, robotId: string): Promise<void> {
        const body = { robotId: robotId }
        await this.POST(path, body).catch((e) => {
            console.error(`Failed to POST /${path}: ` + e)
            throw e
        })
    }

    async getRobots(): Promise<Robot[]> {
        const path: string = 'robots'
        const result = await this.GET<Robot[]>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }

    async getAllEchoMissions(): Promise<EchoMission[]> {
        const path: string = 'echo-missions'
        const result = await this.GET<EchoMission[]>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }

    async getMissions(parameters: MissionQueryParameters): Promise<PaginatedResponse<Mission>> {
        let path: string = 'missions?'
        if (parameters.status) path = path + 'status=' + parameters.status + '&'
        if (parameters.pageNumber) path = path + 'PageNumber=' + parameters.pageNumber + '&'
        if (parameters.pageSize) path = path + 'PageSize=' + parameters.pageSize + '&'
        if (parameters.orderBy) path = path + 'OrderBy=' + parameters.orderBy + '&'
        const result = await this.GET<Mission[]>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        if (!result.headers.has(PaginationHeaderName)) {
            console.error('No Pagination header received ("' + PaginationHeaderName + '")')
        }
        const pagination: PaginationHeader = JSON.parse(result.headers.get(PaginationHeaderName)!)
        return { pagination: pagination, content: result.content }
    }

    async getEchoMissions(installationCode: string = ''): Promise<EchoMission[]> {
        const path: string = 'echo-missions?installationCode=' + installationCode
        const result = await this.GET<EchoMission[]>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }

    async getMissionById(missionId: string): Promise<Mission> {
        const path: string = 'missions/' + missionId
        const result = await this.GET<Mission>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }

    async getVideoStreamsByRobotId(robotId: string): Promise<VideoStream[]> {
        const path: string = 'robots/' + robotId + '/video-streams'
        const result = await this.GET<VideoStream[]>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }

    async getEchoPlantInfo(): Promise<EchoPlantInfo[]> {
        const path: string = 'echo-plants'
        const result = await this.GET<EchoPlantInfo[]>(path).catch((e: Error) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }
    async postMission(echoMissionId: number, robotId: string, assetCode: string | null) {
        const path: string = 'missions'
        const robots: Robot[] = await this.getRobots()
        const desiredRobot = filterRobots(robots, robotId)
        const body = {
            robotId: desiredRobot[0].id,
            echoMissionId: echoMissionId,
            desiredStartTime: new Date(),
            assetCode: assetCode,
        }
        const result = await this.POST<unknown, unknown>(path, body).catch((e) => {
            console.error(`Failed to POST /${path}: ` + e)
            throw e
        })
        return result.content
    }

    async deleteMission(missionId: string) {
        const path: string = 'missions/' + missionId
        await this.DELETE(path, '').catch((e) => {
            console.error(`Failed to DELETE /${path}: ` + e)
            throw e
        })
    }

    async pauseMission(robotId: string): Promise<void> {
        const path: string = 'robots/' + robotId + '/pause'
        return this.postControlMissionRequest(path, robotId)
    }

    async resumeMission(robotId: string): Promise<void> {
        const path: string = 'robots/' + robotId + '/resume'
        return this.postControlMissionRequest(path, robotId)
    }

    async stopMission(robotId: string): Promise<void> {
        const path: string = 'robots/' + robotId + '/stop'
        return this.postControlMissionRequest(path, robotId)
    }

    async getMap(missionId: string): Promise<Blob> {
        const path: string = 'missions/' + missionId + '/map'
        const url = `${config.BACKEND_URL}/${path}`

        const headers = {
            'content-type': 'image/png',
            Authorization: `Bearer ${this.accessToken}`,
        }

        const options: RequestInit = {
            method: 'GET',
            headers,
            mode: 'cors',
        }

        let response = await fetch(url, options)

        if (response.status === 200) {
            const imageBlob = await response.blob()
            return imageBlob
        } else {
            console.log('HTTP-Error: ' + response.status)
            throw Error
        }
    }
}

export const useApi = () => {
    const accessToken = useContext(AccessTokenContext)
    return new BackendAPICaller(accessToken)
}
