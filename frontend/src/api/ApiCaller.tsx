import { config } from 'config'
import { EchoMission, EchoPlantInfo } from 'models/EchoMission'
import { Mission } from 'models/Mission'
import { Robot } from 'models/Robot'
import { VideoStream } from 'models/VideoStream'
import { filterRobots } from 'utils/filtersAndSorts'
import { MissionQueryParameters } from 'models/MissionQueryParameters'
import { PaginatedResponse, PaginationHeader, PaginationHeaderName } from 'models/PaginatedResponse'
import { Pose } from 'models/Pose'
import { AssetDeck } from 'models/AssetDeck'
import { timeout } from 'utils/timeout'

/** Implements the request sent to the backend api. */
export class BackendAPICaller {
    static accessToken: string
    static assetCode: string

    /**  API is not ready until access token has been set for the first time */
    private static async ApiReady() {
        while (this.accessToken === null || this.accessToken === '') {
            await timeout(500)
        }
    }

    private static initializeRequest<T>(method: 'GET' | 'POST' | 'PUT' | 'DELETE', body?: T): RequestInit {
        const headers = {
            'content-type': 'application/json',
            Authorization: `Bearer ${BackendAPICaller.accessToken}`,
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

    private static async query<TBody, TContent>(
        method: 'GET' | 'POST' | 'PUT' | 'DELETE',
        path: string,
        body?: TBody
    ): Promise<{ content: TContent; headers: Headers }> {
        await BackendAPICaller.ApiReady()

        const initializedRequest: RequestInit = BackendAPICaller.initializeRequest(method, body)

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

    private static async GET<TContent>(path: string): Promise<{ content: TContent; headers: Headers }> {
        return BackendAPICaller.query('GET', path)
    }

    private static async POST<TBody, TContent>(
        path: string,
        body: TBody
    ): Promise<{ content: TContent; headers: Headers }> {
        return BackendAPICaller.query('POST', path, body)
    }

    private static async DELETE<TBody, TContent>(
        path: string,
        body: TBody
    ): Promise<{ content: TContent; headers: Headers }> {
        return BackendAPICaller.query('DELETE', path, body)
    }

    private static async postControlMissionRequest(path: string, robotId: string): Promise<void> {
        const body = { robotId: robotId }
        await BackendAPICaller.POST(path, body).catch((e) => {
            console.error(`Failed to POST /${path}: ` + e)
            throw e
        })
    }

    static async getEnabledRobots(): Promise<Robot[]> {
        const path: string = 'robots'
        const result = await BackendAPICaller.GET<Robot[]>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content.filter((robot) => robot.enabled)
    }

    static async getRobotById(robotId: string): Promise<Robot> {
        const path: string = 'robots/' + robotId
        const result = await this.GET<Robot>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async getAllEchoMissions(): Promise<EchoMission[]> {
        const path: string = 'echo-missions'
        const result = await BackendAPICaller.GET<EchoMission[]>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async getMissions(parameters: MissionQueryParameters): Promise<PaginatedResponse<Mission>> {
        let path: string = 'missions?'

        // Always filter by currently selected asset
        const assetCode: string | null = BackendAPICaller.assetCode
        if (assetCode) path = path + 'AssetCode=' + assetCode + '&'

        if (parameters.status) path = path + 'status=' + parameters.status + '&'
        if (parameters.pageNumber) path = path + 'PageNumber=' + parameters.pageNumber + '&'
        if (parameters.pageSize) path = path + 'PageSize=' + parameters.pageSize + '&'
        if (parameters.orderBy) path = path + 'OrderBy=' + parameters.orderBy + '&'
        if (parameters.robotId) path = path + 'RobotId=' + parameters.robotId + '&'
        if (parameters.nameSearch) path = path + 'NameSearch=' + parameters.nameSearch + '&'
        if (parameters.robotNameSearch) path = path + 'RobotNameSearch=' + parameters.robotNameSearch + '&'
        if (parameters.tagSearch) path = path + 'TagSearch=' + parameters.tagSearch + '&'
        if (parameters.minStartTime) path = path + 'MinStartTime=' + parameters.minStartTime + '&'
        if (parameters.maxStartTime) path = path + 'MaxStartTime=' + parameters.maxStartTime + '&'
        if (parameters.minEndTime) path = path + 'MinEndTime=' + parameters.minEndTime + '&'
        if (parameters.maxEndTime) path = path + 'MaxEndTime=' + parameters.maxEndTime + '&'
        if (parameters.minDesiredStartTime) path = path + 'MinDesiredStartTime=' + parameters.minDesiredStartTime + '&'
        if (parameters.maxDesiredStartTime) path = path + 'MaxDesiredStartTime=' + parameters.maxDesiredStartTime + '&'

        const result = await BackendAPICaller.GET<Mission[]>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        if (!result.headers.has(PaginationHeaderName)) {
            console.error('No Pagination header received ("' + PaginationHeaderName + '")')
        }
        const pagination: PaginationHeader = JSON.parse(result.headers.get(PaginationHeaderName)!)
        return { pagination: pagination, content: result.content }
    }

    static async getEchoMissions(installationCode: string = ''): Promise<EchoMission[]> {
        const path: string = 'echo-missions?installationCode=' + installationCode
        const result = await BackendAPICaller.GET<EchoMission[]>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async getMissionById(missionId: string): Promise<Mission> {
        const path: string = 'missions/' + missionId
        const result = await BackendAPICaller.GET<Mission>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async getVideoStreamsByRobotId(robotId: string): Promise<VideoStream[]> {
        const path: string = 'robots/' + robotId + '/video-streams'
        const result = await BackendAPICaller.GET<VideoStream[]>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async getEchoPlantInfo(): Promise<EchoPlantInfo[]> {
        const path: string = 'echo-plants'
        const result = await BackendAPICaller.GET<EchoPlantInfo[]>(path).catch((e: Error) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }
    static async postMission(echoMissionId: number, robotId: string, assetCode: string | null) {
        const path: string = 'missions'
        const robots: Robot[] = await BackendAPICaller.getEnabledRobots()
        const desiredRobot = filterRobots(robots, robotId)
        const body = {
            robotId: desiredRobot[0].id,
            echoMissionId: echoMissionId,
            desiredStartTime: new Date(),
            assetCode: assetCode,
        }
        const result = await BackendAPICaller.POST<unknown, unknown>(path, body).catch((e) => {
            console.error(`Failed to POST /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async postLocalizationMission(localizationPose: Pose, robotId: string) {
        const path: string = 'robots/' + robotId + '/start-localization'
        const body = {
            position: localizationPose.position,
            orientation: localizationPose.orientation,
        }
        const result = await this.POST<unknown, unknown>(path, body).catch((e) => {
            console.error(`Failed to POST /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async deleteMission(missionId: string) {
        const path: string = 'missions/' + missionId
        await BackendAPICaller.DELETE(path, '').catch((e) => {
            console.error(`Failed to DELETE /${path}: ` + e)
            throw e
        })
    }

    static async pauseMission(robotId: string): Promise<void> {
        const path: string = 'robots/' + robotId + '/pause'
        return BackendAPICaller.postControlMissionRequest(path, robotId)
    }

    static async resumeMission(robotId: string): Promise<void> {
        const path: string = 'robots/' + robotId + '/resume'
        return BackendAPICaller.postControlMissionRequest(path, robotId)
    }

    static async stopMission(robotId: string): Promise<void> {
        const path: string = 'robots/' + robotId + '/stop'
        return BackendAPICaller.postControlMissionRequest(path, robotId)
    }

    static async getMap(missionId: string): Promise<Blob> {
        const path: string = 'missions/' + missionId + '/map'
        const url = `${config.BACKEND_URL}/${path}`

        const headers = {
            'content-type': 'image/png',
            Authorization: `Bearer ${BackendAPICaller.accessToken}`,
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
            console.error('HTTP-Error: ' + response.status)
            throw Error
        }
    }

    static async getAssetDecks(): Promise<AssetDeck[]> {
        const path: string = 'asset-decks'
        const result = await this.GET<AssetDeck[]>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }
}
