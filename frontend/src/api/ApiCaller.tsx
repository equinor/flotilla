import { config } from 'config'
import { Mission } from 'models/Mission'
import { Robot } from 'models/Robot'
import { filterRobots } from 'utils/filtersAndSorts'
import { MissionRunQueryParameters } from 'models/MissionRunQueryParameters'
import { MissionDefinitionQueryParameters } from 'models/MissionDefinitionQueryParameters'
import { PaginatedResponse, PaginationHeader, PaginationHeaderName } from 'models/PaginatedResponse'
import { Area } from 'models/Area'
import { timeout } from 'utils/timeout'
import { tokenReverificationInterval } from 'components/Contexts/AuthProvider'
import { MapMetadata } from 'models/MapMetadata'
import { MissionDefinition, PlantInfo } from 'models/MissionDefinition'
import { MissionDefinitionUpdateForm } from 'models/MissionDefinitionUpdateForm'
import { InspectionArea } from 'models/InspectionArea'
import { ApiError, isApiError } from './ApiError'
import { MediaStreamConfig } from 'models/VideoStream'

/** Implements the request sent to the backend api. */
export class BackendAPICaller {
    static accessToken: string
    static installationCode: string

    /**  API is not ready until access token has been set for the first time */
    private static async ApiReady() {
        while (!this.accessToken || this.accessToken === '') {
            await timeout(500)
        }
    }

    private static initializeRequest<T>(
        method: 'GET' | 'POST' | 'PUT' | 'DELETE',
        body?: T,
        contentType?: string
    ): RequestInit {
        const headers = {
            'content-type': contentType ?? 'application/json',
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
        body?: TBody,
        contentType?: string
    ): Promise<{ content: TContent; headers: Headers }> {
        await BackendAPICaller.ApiReady()

        const initializedRequest: RequestInit = BackendAPICaller.initializeRequest(method, body, contentType)

        const url = `${config.BACKEND_URL}/${path}`

        let response: Response
        response = await fetch(url, initializedRequest)

        // If Unauthenticated, token may have expired, wait max token refresh time and try again
        if (response.status === 401) {
            await timeout(tokenReverificationInterval)
            response = await fetch(url, initializedRequest)
        }

        if (!response.ok) throw ApiError.fromCode(response.status, response.statusText, await response.text())

        var responseContent
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
        } else responseContent = ''
        return { content: responseContent, headers: response.headers }
    }

    private static handleError = (requestType: string, path: string) => (e: Error) => {
        if (isApiError(e)) {
            console.error(`Failed to ${requestType} /${path}: ` + (e as ApiError).logMessage)
            throw new Error((e as ApiError).message)
        }

        console.error(`Failed to ${requestType} /${path}: ` + e)
        throw e
    }

    private static async GET<TContent>(
        path: string,
        contentType?: string
    ): Promise<{ content: TContent; headers: Headers }> {
        return BackendAPICaller.query('GET', path, undefined, contentType)
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
        await BackendAPICaller.POST(path, body).catch(BackendAPICaller.handleError('POST', path))
    }

    static async getEnabledRobots(): Promise<Robot[]> {
        const path: string = 'robots'
        const result = await BackendAPICaller.GET<Robot[]>(path).catch(BackendAPICaller.handleError('GET', path))
        return result.content.filter((robot) => !robot.deprecated)
    }

    static async getRobotById(robotId: string): Promise<Robot> {
        const path: string = 'robots/' + robotId
        const result = await this.GET<Robot>(path).catch(BackendAPICaller.handleError('GET', path))
        return result.content
    }

    static async getRobotMediaConfig(robotId: string): Promise<MediaStreamConfig> {
        const path: string = 'media-stream/' + robotId
        const result = await this.GET<MediaStreamConfig>(path).catch(BackendAPICaller.handleError('GET', path))
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

        if (parameters.inspectionArea) path = path + 'InspectionArea=' + parameters.inspectionArea + '&'
        if (parameters.pageNumber) path = path + 'PageNumber=' + parameters.pageNumber + '&'
        if (parameters.pageSize) path = path + 'PageSize=' + parameters.pageSize + '&'
        if (parameters.orderBy) path = path + 'OrderBy=' + parameters.orderBy + '&'
        if (parameters.robotId) path = path + 'RobotId=' + parameters.robotId + '&'
        if (parameters.missionId) path = path + 'MissionId=' + parameters.missionId + '&'
        if (parameters.nameSearch) path = path + 'NameSearch=' + parameters.nameSearch + '&'
        if (parameters.robotNameSearch) path = path + 'RobotNameSearch=' + parameters.robotNameSearch + '&'
        if (parameters.tagSearch) path = path + 'TagSearch=' + parameters.tagSearch + '&'
        if (parameters.excludeLocalization) path = path + 'ExcludeLocalization=' + parameters.excludeLocalization + '&'
        if (parameters.excludeReturnToHome) path = path + 'ExcludeReturnToHome=' + parameters.excludeReturnToHome + '&'
        if (parameters.minStartTime) path = path + 'MinStartTime=' + parameters.minStartTime + '&'
        if (parameters.maxStartTime) path = path + 'MaxStartTime=' + parameters.maxStartTime + '&'
        if (parameters.minEndTime) path = path + 'MinEndTime=' + parameters.minEndTime + '&'
        if (parameters.maxEndTime) path = path + 'MaxEndTime=' + parameters.maxEndTime + '&'
        if (parameters.minDesiredStartTime) path = path + 'MinDesiredStartTime=' + parameters.minDesiredStartTime + '&'
        if (parameters.maxDesiredStartTime) path = path + 'MaxDesiredStartTime=' + parameters.maxDesiredStartTime + '&'

        const result = await BackendAPICaller.GET<Mission[]>(path).catch(BackendAPICaller.handleError('GET', path))
        if (!result.headers.has(PaginationHeaderName)) {
            console.error('No Pagination header received ("' + PaginationHeaderName + '")')
        }
        const pagination: PaginationHeader = JSON.parse(result.headers.get(PaginationHeaderName)!)
        return { pagination: pagination, content: result.content }
    }

    static async getAvailableMissions(installationCode: string = ''): Promise<MissionDefinition[]> {
        const path: string = 'mission-loader/available-missions/' + installationCode
        const result = await BackendAPICaller.GET<MissionDefinition[]>(path).catch(
            BackendAPICaller.handleError('GET', path)
        )
        return result.content
    }

    static async getMissionDefinitions(
        parameters: MissionDefinitionQueryParameters
    ): Promise<PaginatedResponse<MissionDefinition>> {
        let path: string = 'missions/definitions?'

        // Always filter by currently selected installation
        const installationCode: string | null = BackendAPICaller.installationCode
        if (installationCode) path = path + 'InstallationCode=' + installationCode + '&'

        if (parameters.inspectionArea) path = path + 'InspectionArea=' + parameters.inspectionArea + '&'
        if (parameters.sourceId) path = path + 'SourceId=' + parameters.sourceId + '&'
        if (parameters.pageNumber) path = path + 'PageNumber=' + parameters.pageNumber + '&'
        if (parameters.pageSize) path = path + 'PageSize=' + parameters.pageSize + '&'
        if (parameters.orderBy) path = path + 'OrderBy=' + parameters.orderBy + '&'
        if (parameters.nameSearch) path = path + 'NameSearch=' + parameters.nameSearch + '&'

        const result = await BackendAPICaller.GET<MissionDefinition[]>(path).catch(
            BackendAPICaller.handleError('GET', path)
        )
        if (!result.headers.has(PaginationHeaderName)) {
            console.error('No Pagination header received ("' + PaginationHeaderName + '")')
        }
        const pagination: PaginationHeader = JSON.parse(result.headers.get(PaginationHeaderName)!)
        return { pagination: pagination, content: result.content }
    }

    static async getMissionDefinitionsInArea(area: Area): Promise<MissionDefinition[]> {
        let path: string = 'areas/' + area.id + '/mission-definitions'

        const result = await BackendAPICaller.GET<MissionDefinition[]>(path).catch(
            BackendAPICaller.handleError('GET', path)
        )
        return result.content
    }

    static async getMissionDefinitionsInInspectionArea(inspectionArea: InspectionArea): Promise<MissionDefinition[]> {
        let path: string = 'inspectionAreas/' + inspectionArea.id + '/mission-definitions'

        const result = await BackendAPICaller.GET<MissionDefinition[]>(path).catch(
            BackendAPICaller.handleError('GET', path)
        )
        return result.content
    }

    static async updateMissionDefinition(id: string, form: MissionDefinitionUpdateForm): Promise<MissionDefinition> {
        const path: string = 'missions/definitions/' + id
        const result = await BackendAPICaller.PUT<MissionDefinitionUpdateForm, MissionDefinition>(path, form).catch(
            BackendAPICaller.handleError('PUT', path)
        )
        return result.content
    }

    static async deleteMissionDefinition(id: string) {
        const path: string = 'missions/definitions/' + id
        await BackendAPICaller.DELETE(path, '').catch(BackendAPICaller.handleError('DELETE', path))
    }

    static async getMissionDefinitionById(missionId: string): Promise<MissionDefinition> {
        const path: string = 'missions/definitions/' + missionId
        const result = await BackendAPICaller.GET<MissionDefinition>(path).catch(
            BackendAPICaller.handleError('GET', path)
        )
        return result.content
    }

    static async getMissionRunById(missionId: string): Promise<Mission> {
        const path: string = 'missions/runs/' + missionId
        const result = await BackendAPICaller.GET<Mission>(path).catch(BackendAPICaller.handleError('GET', path))
        return result.content
    }

    static async getPlantInfo(): Promise<PlantInfo[]> {
        const path: string = 'mission-loader/plants'
        const result = await BackendAPICaller.GET<PlantInfo[]>(path).catch(BackendAPICaller.handleError('GET', path))
        return result.content
    }

    static async getActivePlants(): Promise<PlantInfo[]> {
        const path: string = 'mission-loader/active-plants'
        const result = await BackendAPICaller.GET<PlantInfo[]>(path).catch(BackendAPICaller.handleError('GET', path))
        return result.content
    }

    static async postMission(missionSourceId: string, robotId: string, installationCode: string | null) {
        const path: string = 'missions'
        const robots: Robot[] = await BackendAPICaller.getEnabledRobots()
        const desiredRobot = filterRobots(robots, robotId)
        const body = {
            robotId: desiredRobot[0].id,
            missionSourceId: missionSourceId,
            installationCode: installationCode,
        }
        const result = await BackendAPICaller.POST<unknown, unknown>(path, body).catch(
            BackendAPICaller.handleError('POST', path)
        )
        return result.content
    }

    static async scheduleMissionDefinition(missionDefinitionId: string, robotId: string): Promise<Mission> {
        const path: string = `missions/schedule/${missionDefinitionId}`
        const robots: Robot[] = await BackendAPICaller.getEnabledRobots()
        const desiredRobot = filterRobots(robots, robotId)
        const body = {
            robotId: desiredRobot[0].id,
        }
        const result = await BackendAPICaller.POST<unknown, Mission>(path, body).catch(
            BackendAPICaller.handleError('POST', path)
        )
        return result.content
    }

    static async setArmPosition(robotId: string, armPosition: string): Promise<void> {
        const path: string = `robots/${robotId}/SetArmPosition/${armPosition}`
        await BackendAPICaller.PUT(path).catch(BackendAPICaller.handleError('PUT', path))
    }

    static async deleteMission(missionId: string) {
        const path: string = 'missions/runs/' + missionId
        return await BackendAPICaller.DELETE(path, '').catch(BackendAPICaller.handleError('DELETE', path))
    }

    static async pauseMission(robotId: string): Promise<void> {
        const path: string = 'robots/' + robotId + '/pause'
        return BackendAPICaller.postControlMissionRequest(path, robotId).catch(
            BackendAPICaller.handleError('POST', path)
        )
    }

    static async resumeMission(robotId: string): Promise<void> {
        const path: string = 'robots/' + robotId + '/resume'
        return BackendAPICaller.postControlMissionRequest(path, robotId).catch(
            BackendAPICaller.handleError('POST', path)
        )
    }

    static async stopMission(robotId: string): Promise<void> {
        const path: string = 'robots/' + robotId + '/stop'
        return BackendAPICaller.postControlMissionRequest(path, robotId).catch(
            BackendAPICaller.handleError('POST', path)
        )
    }

    static async getMap(installationCode: string, mapName: string): Promise<Blob> {
        const path: string = 'missions/' + installationCode + '/' + mapName + '/map'

        return BackendAPICaller.GET<Blob>(path, 'image/png')
            .then((response) => response.content)
            .catch(BackendAPICaller.handleError('GET', path))
    }

    static async getAreas(): Promise<Area[]> {
        const path: string = 'areas'
        const result = await this.GET<Area[]>(path).catch(BackendAPICaller.handleError('GET', path))
        return result.content
    }

    static async getAreasByInspectionAreaId(inspectionAreaId: string): Promise<Area[]> {
        const path: string = 'areas/inspectionArea/' + inspectionAreaId
        const result = await this.GET<Area[]>(path).catch(BackendAPICaller.handleError('GET', path))
        return result.content
    }

    static async getInspectionAreas(): Promise<InspectionArea[]> {
        const path: string = 'inspectionAreas'
        const result = await this.GET<InspectionArea[]>(path).catch(BackendAPICaller.handleError('GET', path))
        return result.content
    }

    static async getInspectionAreasByInstallationCode(installationCode: string): Promise<InspectionArea[]> {
        const path: string = 'inspectionAreas/installation/' + installationCode
        const result = await this.GET<InspectionArea[]>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async getInspectionAreaMapMetadata(id: string): Promise<MapMetadata> {
        const path: string = 'inspectionAreas/' + id + '/map-metadata'
        const result = await this.GET<MapMetadata>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async reRunMission(missionId: string, failedTasksOnly: boolean = false): Promise<Mission> {
        let mission = await this.getMissionRunById(missionId)

        if (failedTasksOnly) {
            const path = `missions/rerun/${mission.id}`
            const body = {
                robotId: mission.robot.id,
            }
            const result = await BackendAPICaller.POST<unknown, Mission>(path, body).catch((e) => {
                console.error(`Failed to POST /${path}: ` + e)
                throw e
            })
            return result.content
        } else {
            return BackendAPICaller.scheduleMissionDefinition(mission.missionId!, mission.robot.id)
        }
    }

    static async sendRobotsToDockingPosition(installationCode: string) {
        const path: string = `emergency-action/${installationCode}/abort-current-missions-and-send-all-robots-to-safe-zone`
        const body = {}

        const result = await this.POST<unknown, unknown>(path, body).catch(BackendAPICaller.handleError('POST', path))
        return result.content
    }

    static async clearEmergencyState(installationCode: string) {
        const path: string = `emergency-action/${installationCode}/clear-emergency-state`
        const body = {}

        const result = await this.POST<unknown, unknown>(path, body).catch(BackendAPICaller.handleError('POST', path))
        return result.content
    }

    static async returnRobotToHome(robotId: string) {
        const path: string = `return-to-home/schedule-return-to-home/` + robotId
        const body = {}

        const result = await BackendAPICaller.POST<unknown, unknown>(path, body).catch((e) => {
            console.error(`Failed to POST /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async getInspection(installationCode: string, taskId: string): Promise<Blob> {
        const path: string = 'inspection/' + installationCode + '/' + taskId + '/taskId'

        return BackendAPICaller.GET<Blob>(path, 'image/png')
            .then((response) => response.content)
            .catch(BackendAPICaller.handleError('GET', path))
    }
}
