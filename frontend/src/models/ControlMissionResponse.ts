import { MissionResponse } from 'components/Pages/FrontPage/MissionOverview/MissionControlButtons'

export interface IControlMissionResponse {
    mission_id: string
    mission_status: MissionResponse
    task_id: string
    task_status: MissionResponse
    step_id: string
    step_status: MissionResponse
}

export class ControlMissionResponse {
    missionId: string
    missionStatus: MissionResponse
    taskId: string
    taskStatus: MissionResponse
    stepId: string
    stepStatus: MissionResponse

    constructor(controlMissionResponseInterface: IControlMissionResponse) {
        this.missionId = controlMissionResponseInterface.mission_id
        this.missionStatus = controlMissionResponseInterface.mission_status
        this.taskId = controlMissionResponseInterface.task_id
        this.taskStatus = controlMissionResponseInterface.task_status
        this.stepId = controlMissionResponseInterface.step_id
        this.stepStatus = controlMissionResponseInterface.step_status
    }
}
