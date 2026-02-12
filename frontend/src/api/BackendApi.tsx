import { RobotWithoutTelemetry } from 'models/Robot'
import { BackendAPICaller } from './ApiCaller'
import { handleError } from './ApiError'
import { MediaStreamConfig } from 'models/VideoStream'
import { MissionRunQueryParameters } from 'models/MissionRunQueryParameters'
import { PaginatedResponse, PaginationHeader, PaginationHeaderName } from 'models/PaginatedResponse'
import { Mission } from 'models/Mission'
import { CondensedMissionDefinition } from 'models/CondensedMissionDefinition'
import { MissionDefinition } from 'models/MissionDefinition'
import { MissionDefinitionQueryParameters } from 'models/MissionDefinitionQueryParameters'
import { InspectionArea } from 'models/InspectionArea'
import { MissionDefinitionUpdateForm } from 'models/MissionDefinitionUpdateForm'
import { filterRobots } from 'utils/filtersAndSorts'
import { PointillaMapInfo } from 'models/PointillaMapInfo'

export class BackendApi {
    constructor(
        private readonly api: BackendAPICaller,
        private readonly installationCode: string | null
    ) {}

    async postControlMissionRequest(path: string, robotId: string): Promise<void> {
        const body = { robotId: robotId }
        await this.api.POST(path, body).catch(handleError('POST', path))
    }

    async getEnabledRobots(): Promise<RobotWithoutTelemetry[]> {
        const { content } = await this.api.GET<RobotWithoutTelemetry[]>('robots')
        return content.filter((robot) => !robot.deprecated)
    }

    async getRobotById(robotId: string): Promise<RobotWithoutTelemetry> {
        const path: string = 'robots/' + robotId
        const result = await this.api.GET<RobotWithoutTelemetry>(path).catch(handleError('GET', path))
        return result.content
    }

    async getRobotMediaConfig(robotId: string): Promise<MediaStreamConfig | null | undefined> {
        const path: string = 'media-stream/' + robotId
        const result = await this.api.GET<MediaStreamConfig>(path).catch(handleError('GET', path))
        return result.content
    }

    async getMissionRuns(parameters: MissionRunQueryParameters): Promise<PaginatedResponse<Mission>> {
        let path: string = 'missions/runs?'

        // Always filter by currently selected installation
        if (this.installationCode) path = path + 'InstallationCode=' + this.installationCode + '&'

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
        if (parameters.minStartTime) path = path + 'MinStartTime=' + parameters.minStartTime + '&'
        if (parameters.maxStartTime) path = path + 'MaxStartTime=' + parameters.maxStartTime + '&'
        if (parameters.minEndTime) path = path + 'MinEndTime=' + parameters.minEndTime + '&'
        if (parameters.maxEndTime) path = path + 'MaxEndTime=' + parameters.maxEndTime + '&'
        if (parameters.minCreationTime) path = path + 'MinCreationTime=' + parameters.minCreationTime + '&'
        if (parameters.maxCreationTime) path = path + 'MaxCreationTime=' + parameters.maxCreationTime + '&'

        const result = await this.api.GET<Mission[]>(path).catch(handleError('GET', path))
        if (!result.headers.has(PaginationHeaderName)) {
            console.error('No Pagination header received ("' + PaginationHeaderName + '")')
        }
        const pagination: PaginationHeader = JSON.parse(result.headers.get(PaginationHeaderName)!)
        return { pagination: pagination, content: result.content }
    }

    async getAvailableMissions(installationCode: string = ''): Promise<CondensedMissionDefinition[]> {
        const path: string = 'mission-loader/available-missions/' + installationCode
        const result = await this.api.GET<MissionDefinition[]>(path).catch(handleError('GET', path))
        return result.content
    }

    async getMissionDefinitions(
        parameters: MissionDefinitionQueryParameters
    ): Promise<PaginatedResponse<MissionDefinition>> {
        let path: string = 'missions/definitions?'

        // Always filter by currently selected installation
        if (this.installationCode) path = path + 'InstallationCode=' + this.installationCode + '&'

        if (parameters.inspectionArea) path = path + 'InspectionArea=' + parameters.inspectionArea + '&'
        if (parameters.sourceId) path = path + 'SourceId=' + parameters.sourceId + '&'
        if (parameters.pageNumber) path = path + 'PageNumber=' + parameters.pageNumber + '&'
        if (parameters.pageSize) path = path + 'PageSize=' + parameters.pageSize + '&'
        if (parameters.orderBy) path = path + 'OrderBy=' + parameters.orderBy + '&'
        if (parameters.nameSearch) path = path + 'NameSearch=' + parameters.nameSearch + '&'

        const result = await this.api.GET<MissionDefinition[]>(path).catch(handleError('GET', path))
        if (!result.headers.has(PaginationHeaderName)) {
            console.error('No Pagination header received ("' + PaginationHeaderName + '")')
        }
        const pagination: PaginationHeader = JSON.parse(result.headers.get(PaginationHeaderName)!)
        return { pagination: pagination, content: result.content }
    }

    async getMissionDefinitionsInInspectionArea(inspectionArea: InspectionArea): Promise<MissionDefinition[]> {
        const path: string = 'inspectionAreas/' + inspectionArea.id + '/mission-definitions'

        const result = await this.api.GET<MissionDefinition[]>(path).catch(handleError('GET', path))
        return result.content
    }

    async updateMissionDefinition(id: string, form: MissionDefinitionUpdateForm): Promise<MissionDefinition> {
        const path: string = 'missions/definitions/' + id
        const result = await this.api
            .PUT<MissionDefinitionUpdateForm, MissionDefinition>(path, form)
            .catch(handleError('PUT', path))
        return result.content
    }

    async deleteMissionDefinition(id: string) {
        const path: string = 'missions/definitions/' + id
        await this.api.DELETE(path, '').catch(handleError('DELETE', path))
    }

    async getMissionDefinitionById(missionId: string): Promise<MissionDefinition> {
        const path: string = 'missions/definitions/' + missionId
        const result = await this.api.GET<MissionDefinition>(path).catch(handleError('GET', path))
        return result.content
    }

    async getMissionRunById(missionId: string): Promise<Mission> {
        const path: string = 'missions/runs/' + missionId
        const result = await this.api.GET<Mission>(path).catch(handleError('GET', path))
        return result.content
    }

    async getMissionRunByIsarInspectionId(inspectionId: string): Promise<Mission> {
        const path: string = 'missions/runs/inspection/' + inspectionId
        const result = await this.api.GET<Mission>(path).catch(handleError('GET', path))
        return result.content
    }

    async postMission(missionSourceId: string, robotId: string, installationCode: string | null) {
        const path: string = 'missions'
        const robots: RobotWithoutTelemetry[] = await this.getEnabledRobots()
        const desiredRobot = filterRobots(robots, robotId)
        const body = {
            robotId: desiredRobot[0].id,
            missionSourceId: missionSourceId,
            installationCode: installationCode,
        }
        const result = await this.api.POST<unknown, unknown>(path, body).catch(handleError('POST', path))
        return result.content
    }

    async scheduleMissionDefinition(missionDefinitionId: string, robotId: string): Promise<Mission> {
        const path: string = `missions/schedule/${missionDefinitionId}`
        const robots: RobotWithoutTelemetry[] = await this.getEnabledRobots()
        const desiredRobot = filterRobots(robots, robotId)
        const body = {
            robotId: desiredRobot[0].id,
        }
        const result = await this.api.POST<unknown, Mission>(path, body).catch(handleError('POST', path))
        return result.content
    }

    async deleteMission(missionId: string) {
        const path: string = 'missions/runs/' + missionId
        return await this.api.DELETE(path, '').catch(handleError('DELETE', path))
    }

    async deleteAllMissions() {
        const path: string = 'missions/runs/queued-missions'
        return await this.api.DELETE(path, '').catch(handleError('DELETE', path))
    }

    async pauseMission(robotId: string): Promise<void> {
        const path: string = 'robots/' + robotId + '/pause'
        return this.postControlMissionRequest(path, robotId).catch(handleError('POST', path))
    }

    async resumeMission(robotId: string): Promise<void> {
        const path: string = 'robots/' + robotId + '/resume'
        return this.postControlMissionRequest(path, robotId).catch(handleError('POST', path))
    }

    async stopMission(robotId: string): Promise<void> {
        const path: string = 'robots/' + robotId + '/stop'
        return this.postControlMissionRequest(path, robotId).catch(handleError('POST', path))
    }

    async getInspectionAreas(): Promise<InspectionArea[]> {
        const path: string = 'inspectionAreas'
        const result = await this.api.GET<InspectionArea[]>(path).catch(handleError('GET', path))
        return result.content
    }

    async getInspectionAreasByInstallationCode(installationCode: string): Promise<InspectionArea[]> {
        const path: string = 'inspectionAreas/installation/' + installationCode
        const result = await this.api.GET<InspectionArea[]>(path).catch(handleError('GET', path))
        return result.content
    }

    async getInspectionAreaById(id: string): Promise<InspectionArea> {
        const path: string = 'inspectionAreas/' + id
        const result = await this.api.GET<InspectionArea>(path).catch(handleError('GET', path))
        return result.content
    }

    async reRunMission(missionId: string, failedTasksOnly: boolean = false): Promise<Mission> {
        const mission = await this.getMissionRunById(missionId)

        if (failedTasksOnly) {
            const path = `missions/rerun/${mission.id}`
            const body = {
                robotId: mission.robot.id,
            }
            const result = await this.api.POST<unknown, Mission>(path, body).catch(handleError('POST', path))
            return result.content
        } else {
            return this.scheduleMissionDefinition(mission.missionId!, mission.robot.id)
        }
    }

    async sendRobotsToDockingPosition(installationCode: string) {
        const path: string = `emergency-action/${installationCode}/abort-current-missions-and-send-all-robots-to-safe-zone`
        const body = {}

        const result = await this.api.POST<unknown, unknown>(path, body).catch(handleError('POST', path))
        return result.content
    }

    async clearEmergencyState(installationCode: string) {
        const path: string = `emergency-action/${installationCode}/clear-emergency-state`
        const body = {}

        const result = await this.api.POST<unknown, unknown>(path, body).catch(handleError('POST', path))
        return result.content
    }

    async returnRobotToHome(robotId: string) {
        const path: string = `return-to-home/schedule-return-to-home/` + robotId
        const body = {}

        const result = await this.api.POST<unknown, unknown>(path, body).catch(handleError('POST', path))
        return result.content
    }

    async setMaintenanceMode(robotId: string) {
        const path: string = `robots/set-maintenance-mode/` + robotId
        const body = {}

        const result = await this.api.POST<unknown, unknown>(path, body).catch(handleError('POST', path))
        return result.content
    }

    async releaseMaintenanceMode(robotId: string) {
        const path: string = `robots/release-maintenance-mode/` + robotId
        const body = {}

        const result = await this.api.POST<unknown, unknown>(path, body).catch(handleError('POST', path))
        return result.content
    }

    async getInspection(inspectionId: string): Promise<Blob> {
        const path: string = 'inspection/' + inspectionId

        return this.api
            .GET<Blob>(path, 'image/png')
            .then((response) => response.content)
            .catch(handleError('GET', path))
    }

    async getAnalysis(inspectionId: string): Promise<Blob> {
        const path: string = 'inspection/analysis/' + inspectionId

        return this.api
            .GET<Blob>(path, 'image/png')
            .then((response) => response.content)
            .catch(handleError('GET', path))
    }

    async skipAutoScheduledMission(missionId: string, timeOfDay: string): Promise<void> {
        const path: string = `missions/definitions/${missionId}/skip-auto-mission`
        const body = { timeOfDay: timeOfDay }

        await this.api.PUT(path, body).catch(handleError('POST', path))
    }

    async releaseInterventionNeeded(robotId: string): Promise<void> {
        const path: string = `robots/${robotId}/release-intervention-needed`
        await this.api.POST(path, {}).catch(handleError('POST', path))
    }

    async getFloorMapTiles(plantCode: string, floorId: string, zoomLevel: number, x: number, y: number): Promise<Blob> {
        const path: string = `pointilla/map/tiles/${plantCode}/${floorId}/${zoomLevel}/${x}/${y}`
        return await this.api
            .GET_BLOB(path)
            .then((r) => r.content)
            .catch(handleError('GET', path))
    }

    async getFloorMapInfo(plantCode: string, floorId: string): Promise<PointillaMapInfo> {
        const path: string = `pointilla/map/${plantCode}/${floorId}`
        return await this.api
            .GET<PointillaMapInfo>(path)
            .then((response) => response.content)
            .catch(handleError('GET', path))
    }

    async getFloorMapTileByPath(
        path: string,
        opts?: { headers?: Record<string, string>; signal?: AbortSignal }
    ): Promise<Blob> {
        return await this.api
            .GET_BLOB(path, opts)
            .then((response) => response.content)
            .catch(handleError('GET', path))
    }
}
