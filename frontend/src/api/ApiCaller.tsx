import { AccessTokenContext } from 'components/Pages/FlotillaSite'
import { config } from 'config'
import { ControlMissionResponse, IControlMissionResponse } from 'models/ControlMissionResponse'
import { EchoMission, EchoPlantInfo } from 'models/EchoMission'
import { Mission, MissionStatus } from 'models/Mission'
import { Robot } from 'models/Robot'
import { VideoStream } from 'models/VideoStream'
import { useContext } from 'react'
import { filterRobots } from 'utils/filtersAndSorts'

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
        const responseContent = await response.json().catch((e) => {
            throw new Error(`Error getting json from response: ${e}`)
        })
        return { content: responseContent, headers: response.headers }
    }

    private async postControlMissionRequest(path: string, robotId: string): Promise<ControlMissionResponse> {
        const body = { robotId: robotId }
        const response = await this.query('POST', path, body).catch((e) => {
            console.error(`Failed to POST /${path}: ` + e)
            throw e
        })

        const responseObject: ControlMissionResponse = new ControlMissionResponse(
            response.content as IControlMissionResponse
        )
        return responseObject
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

    async getMissions(): Promise<Mission[]> {
        const path: string = 'missions'
        const result = await this.GET<Mission[]>(path).catch((e) => {
            throw new Error(`Failed to GET /${path}: ` + e)
        })
        return result.content
    }

    async getMissionsByStatus(status: MissionStatus): Promise<Mission[]> {
        const path: string = 'missions?status=' + status
        const result = await this.GET<Mission[]>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
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

    async pauseMission(robotId: string): Promise<ControlMissionResponse> {
        const path: string = 'robots/' + robotId + '/pause'
        return this.postControlMissionRequest(path, robotId)
    }

    async resumeMission(robotId: string): Promise<ControlMissionResponse> {
        const path: string = 'robots/' + robotId + '/resume'
        return this.postControlMissionRequest(path, robotId)
    }

    async stopMission(robotId: string): Promise<ControlMissionResponse> {
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
