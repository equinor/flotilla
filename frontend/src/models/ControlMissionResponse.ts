import { MissionResponse } from 'components/Pages/FrontPage/MissionOverview/MissionControlButtons'

export interface ControlMissionResponse {
    mission_id: string
    mission_status: MissionResponse
    task_id: string
    task_status: MissionResponse
    step_id: string
    step_status: MissionResponse
}
