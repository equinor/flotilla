import { Area } from "./Area"
import { Mission } from "./Mission"
import { Task } from "./Task"

export interface EchoMissionDefinition {
    echoMissionId: number
    name: string
}

export interface MissionDefinition {
    id: string
    tasks: Task[]
    name: string
    installationCode: string
    comment: string
    inspectionFrequency: string
    lastRun: Mission
    area: Area
}
