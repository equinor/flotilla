import { config } from 'config'
import { EchoPlantInfo } from 'models/EchoMission'
import { Mission } from 'models/Mission'
import { Robot } from 'models/Robot'
import { VideoStream } from 'models/VideoStream'
import { filterRobots } from 'utils/filtersAndSorts'
import { MissionRunQueryParameters } from 'models/MissionRunQueryParameters'
import { MissionDefinitionQueryParameters, SourceType } from 'models/MissionDefinitionQueryParameters'
import { PaginatedResponse, PaginationHeader, PaginationHeaderName } from 'models/PaginatedResponse'
import { Pose } from 'models/Pose'
import { Area } from 'models/Area'
import { timeout } from 'utils/timeout'
import { tokenReverificationInterval } from 'components/Contexts/AuthProvider'
import { TaskStatus } from 'models/Task'
import { CreateCustomMission, CustomMissionQuery } from 'models/CustomMission'
import { MapMetadata } from 'models/MapMetadata'
import { MissionDefinition } from 'models/MissionDefinition'
import { EchoMission } from 'models/EchoMission'

/** Implements the request sent to the backend api. */
export class BackendAPICaller {
    static accessToken: string
    static installationCode: string

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

        let response: Response
        response = await fetch(url, initializedRequest)

        // If Unauthenticated, token may have expired, wait max token refresh time and try again
        if (response.status === 401) {
            await timeout(tokenReverificationInterval)
            response = await fetch(url, initializedRequest)
        }

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
    private static async PUT<TBody, TContent>(
        path: string,
        body?: TBody
    ): Promise<{ content: TContent; headers: Headers }> {
        return BackendAPICaller.query('PUT', path, body)
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
        const path: string = 'echo/missions'
        const result = await BackendAPICaller.GET<EchoMission[]>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async getMissionRuns(parameters: MissionRunQueryParameters): Promise<PaginatedResponse<Mission>> {
        let path: string = 'missions/runs?'

        // Always filter by currently selected installation
        const installationCode: string | null = BackendAPICaller.installationCode
        if (installationCode) path = path + 'InstallationCode=' + installationCode + '&'

        if (parameters.statuses) {
            parameters.statuses.forEach((status) => {
                path = path + 'Statuses=' + status + '&'
            })
        }
        if (parameters.inspectionTypes) {
            parameters.inspectionTypes.forEach((inspectionType) => {
                path = path + 'InspectionTypes=' + inspectionType + '&'
            })
        }

        if (parameters.area) path = path + 'Area=' + parameters.area + '&'
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

    static async getAvailableEchoMission(installationCode: string = ''): Promise<MissionDefinition[]> {
        const path: string = 'echo/available-missions?installationCode=' + installationCode
        const result = await BackendAPICaller.GET<MissionDefinition[]>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async getMissionDefinitions(
        parameters: MissionDefinitionQueryParameters
    ): Promise<PaginatedResponse<Mission>> {
        let path: string = 'missions/definitions?'

        // Always filter by currently selected installation
        const installationCode: string | null = BackendAPICaller.installationCode
        if (installationCode) path = path + 'InstallationCode=' + installationCode + '&'

        if (parameters.area) path = path + 'Area=' + parameters.area + '&'
        if (parameters.sourceType) path = path + 'SourceType=' + parameters.sourceType + '&'
        if (parameters.pageNumber) path = path + 'PageNumber=' + parameters.pageNumber + '&'
        if (parameters.pageSize) path = path + 'PageSize=' + parameters.pageSize + '&'
        if (parameters.orderBy) path = path + 'OrderBy=' + parameters.orderBy + '&'
        if (parameters.nameSearch) path = path + 'NameSearch=' + parameters.nameSearch + '&'
        if (parameters.sourceType) path = path + 'SourceType=' + parameters.sourceType + '&'

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
        const path: string = 'echo/missions?installationCode=' + installationCode
        const result = await BackendAPICaller.GET<EchoMission[]>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async getMissionRunById(missionId: string): Promise<Mission> {
        const path: string = 'missions/runs/' + missionId
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
        const path: string = 'echo/plants'
        const result = await BackendAPICaller.GET<EchoPlantInfo[]>(path).catch((e: Error) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }
    static async postMission(echoMissionId: number, robotId: string, installationCode: string | null) {
        const path: string = 'missions'
        const robots: Robot[] = await BackendAPICaller.getEnabledRobots()
        const desiredRobot = filterRobots(robots, robotId)
        const body = {
            robotId: desiredRobot[0].id,
            echoMissionId: echoMissionId,
            desiredStartTime: new Date(),
            installationCode: installationCode,
            areaName: '', // TODO: we need a way of populating the area database, then including area in MissionDefinition
        }
        const result = await BackendAPICaller.POST<unknown, unknown>(path, body).catch((e) => {
            console.error(`Failed to POST /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async postLocalizationMission(localizationPose: Pose, robotId: string, areaId: string) {
        const path: string = 'robots/start-localization'
        const body = {
            robotId: robotId,
            localizationPose: localizationPose,
            areaId: areaId,
        }
        const result = await this.POST<unknown, unknown>(path, body).catch((e) => {
            console.error(`Failed to POST /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async setArmPosition(robotId: string, armPosition: string): Promise<void> {
        const path: string = `robots/${robotId}/SetArmPosition/${armPosition}`
        await BackendAPICaller.PUT(path).catch((e) => {
            console.error(`Failed to PUT /${path}: ` + e)
            throw e
        })
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

    static async getMap(installationCode: string, mapName: string): Promise<Blob> {
        const path: string = 'missions/' + installationCode + '/' + mapName + '/map'
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

    static async getAreas(): Promise<Area[]> {
        const path: string = 'areas'
        const result = await this.GET<Area[]>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async getAreasMapMetadata(id: string): Promise<MapMetadata> {
        const path: string = 'areas/' + id + '/map-metadata'
        const result = await this.GET<MapMetadata>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async reRunMission(missionId: string, failedTasksOnly: boolean = false): Promise<Mission> {
        let mission = await this.getMissionRunById(missionId)

        // TODO: utilise reschedule endpoint instead of copying

        if (failedTasksOnly) {
            mission.tasks = mission.tasks.filter(
                (task) => task.status !== TaskStatus.PartiallySuccessful && task.status !== TaskStatus.Successful
            )
            // Fix task ordering
            for (let index = 0; index < mission.tasks.length; index++) {
                mission.tasks[index].taskOrder = index
            }
        }

        const customMission = CreateCustomMission(mission)

        const path: string = 'missions/custom'
        const body = customMission
        const result = await BackendAPICaller.POST<CustomMissionQuery, Mission>(path, body).catch((e) => {
            console.error(`Failed to POST /${path}: ` + e)
            throw e
        })
        return result.content
    }
}
