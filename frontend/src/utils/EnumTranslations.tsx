import { InspectionType } from 'models/Inspection'
import { Mission, MissionStatus } from 'models/Mission'
import { TaskStatus } from 'models/Task'

export const translateSignalRMission = (signalRMission: Mission) => {
    // This conversion translates from the enum as a number to an enum as a string
    signalRMission.status = Object.values(MissionStatus)[signalRMission.status as unknown as number]
    signalRMission.tasks = signalRMission.tasks.map((t) => {
        return {
            ...t,
            inspections: t.inspections.map((i) => {
                return {
                    ...i,
                    inspectionType: Object.values(InspectionType)[i.inspectionType as unknown as number],
                }
            }),
            status: Object.values(TaskStatus)[t.status as unknown as number],
        }
    })
    return signalRMission
}
