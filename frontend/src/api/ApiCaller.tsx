import { config } from 'config'
import { EchoPlantInfo } from 'models/EchoMission'
import { Mission } from 'models/Mission'
import { Robot } from 'models/Robot'
import { VideoStream } from 'models/VideoStream'
import { filterRobots } from 'utils/filtersAndSorts'
import { MissionRunQueryParameters } from 'models/MissionRunQueryParameters'
import { MissionDefinitionQueryParameters } from 'models/MissionDefinitionQueryParameters'
import { PaginatedResponse, PaginationHeader, PaginationHeaderName } from 'models/PaginatedResponse'
import { Pose } from 'models/Pose'
import { Area } from 'models/Area'
import { timeout } from 'utils/timeout'
import { tokenReverificationInterval } from 'components/Contexts/AuthProvider'
import { MapMetadata } from 'models/MapMetadata'
import { CondensedMissionDefinition, EchoMissionDefinition } from 'models/MissionDefinition'
import { EchoMission } from 'models/EchoMission'
import { MissionDefinitionUpdateForm } from 'models/MissionDefinitionUpdateForm'
import { Deck } from 'models/Deck'

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

        if (!response.ok) throw new Error(`${response.status} - ${response.statusText}`)
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
        if (parameters.missionId) path = path + 'MissionId=' + parameters.missionId + '&'
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

    static async getAvailableEchoMissions(installationCode: string = ''): Promise<EchoMissionDefinition[]> {
        const path: string = 'echo/available-missions/' + installationCode
        const result = await BackendAPICaller.GET<EchoMissionDefinition[]>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async getMissionDefinitions(
        parameters: MissionDefinitionQueryParameters
    ): Promise<PaginatedResponse<CondensedMissionDefinition>> {
        let path: string = 'missions/definitions/condensed?'

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

        const result = await BackendAPICaller.GET<CondensedMissionDefinition[]>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        if (!result.headers.has(PaginationHeaderName)) {
            console.error('No Pagination header received ("' + PaginationHeaderName + '")')
        }
        const pagination: PaginationHeader = JSON.parse(result.headers.get(PaginationHeaderName)!)
        return { pagination: pagination, content: result.content }
    }

    static async getMissionDefinitionsInArea(area: Area): Promise<CondensedMissionDefinition[]> {
        let path: string = 'areas/' + area.id + '/mission-definitions'

        const result = await BackendAPICaller.GET<CondensedMissionDefinition[]>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async getMissionDefinitionsInDeck(deck: Deck): Promise<CondensedMissionDefinition[]> {
        let path: string = 'decks/' + deck.id + '/mission-definitions'

        const result = await BackendAPICaller.GET<CondensedMissionDefinition[]>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async updateMissionDefinition(
        id: string,
        form: MissionDefinitionUpdateForm
    ): Promise<CondensedMissionDefinition> {
        const path: string = 'missions/definitions/' + id
        const result = await BackendAPICaller.PUT<MissionDefinitionUpdateForm, CondensedMissionDefinition>(
            path,
            form
        ).catch((e) => {
            console.error(`Failed to PUT /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async deleteMissionDefinition(id: string) {
        const path: string = 'missions/definitions/' + id
        await BackendAPICaller.DELETE(path, '').catch((e) => {
            console.error(`Failed to DELETE /${path}: ` + e)
            throw e
        })
    }

    static async getMissionDefinitionById(missionId: string): Promise<CondensedMissionDefinition> {
        const path: string = 'missions/definitions/' + missionId + '/condensed'
        const result = await BackendAPICaller.GET<CondensedMissionDefinition>(path).catch((e) => {
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

    static async getActivePlants(): Promise<EchoPlantInfo[]> {
        const path: string = 'echo/active-plants'
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
            areaName: '',
        }
        const result = await BackendAPICaller.POST<unknown, unknown>(path, body).catch((e) => {
            console.error(`Failed to POST /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async scheduleMissionDefinition(missionDefinitionId: string, robotId: string): Promise<Mission> {
        const path: string = `missions/schedule/${missionDefinitionId}`
        const robots: Robot[] = await BackendAPICaller.getEnabledRobots()
        const desiredRobot = filterRobots(robots, robotId)
        const body = {
            robotId: desiredRobot[0].id,
        }
        const result = await BackendAPICaller.POST<unknown, Mission>(path, body).catch((e) => {
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
        const path: string = 'missions/runs/' + missionId
        return await BackendAPICaller.DELETE(path, '').catch((e) => {
            console.error(`Failed to DELETE /${path}: ` + e)
            throw e
        })
    }

    static async pauseMission(robotId: string): Promise<void> {
        const path: string = 'robots/' + robotId + '/pause'
        return BackendAPICaller.postControlMissionRequest(path, robotId).catch((e) => {
            console.error(`Failed to POST /${path}: ` + e)
            throw e
        })
    }

    static async resumeMission(robotId: string): Promise<void> {
        const path: string = 'robots/' + robotId + '/resume'
        return BackendAPICaller.postControlMissionRequest(path, robotId).catch((e) => {
            console.error(`Failed to POST /${path}: ` + e)
            throw e
        })
    }

    static async stopMission(robotId: string): Promise<void> {
        const path: string = 'robots/' + robotId + '/stop'
        return BackendAPICaller.postControlMissionRequest(path, robotId).catch((e) => {
            console.error(`Failed to POST /${path}: ` + e)
            throw e
        })
    }

    static async getMap(installationCode: string, mapName: string): Promise<Blob> {
        const path: string = 'missions/' + installationCode + '/' + mapName + '/map'

        return BackendAPICaller.GET<Blob>(path, 'image/png')
            .then((response) => response.content)
            .catch((e) => {
                console.error(`Failed to GET /${path}: ` + e)
                throw e
            })
    }

    static async getAreas(): Promise<Area[]> {
        const path: string = 'areas'
        const result = await this.GET<Area[]>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async getAreasByDeckId(deckId: string): Promise<Area[]> {
        const path: string = 'areas/deck/' + deckId
        const result = await this.GET<Area[]>(path).catch((e) => {
            console.error(`Failed to GET /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async getDecks(): Promise<Deck[]> {
        const path: string = 'decks'
        const result = await this.GET<Deck[]>(path).catch((e) => {
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

    static async getDeckMapMetadata(id: string): Promise<MapMetadata> {
        const path: string = 'decks/' + id + '/map-metadata'
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

    static async sendRobotsToSafePosition(installationCode: string) {
        const path: string = `emergency-action/${installationCode}/abort-current-missions-and-send-all-robots-to-safe-zone`
        const body = {}

        const result = await this.POST<unknown, unknown>(path, body).catch((e) => {
            console.error(`Failed to POST /${path}: ` + e)
            throw e
        })
        return result.content
    }

    static async clearEmergencyState(installationCode: string) {
        const path: string = `emergency-action/${installationCode}/clear-emergency-state`
        const body = {}

        const result = await this.POST<unknown, unknown>(path, body).catch((e) => {
            console.error(`Failed to POST /${path}: ` + e)
            throw e
        })
        return result.content
    }
}
