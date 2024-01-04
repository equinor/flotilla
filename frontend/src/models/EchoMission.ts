export interface EchoMission {
    id: number
    name: string
    url: string
    tags: EchoTag[]
}

interface EchoTag {
    id: number
    tagId: string
    url: string
    inspections: EchoInspection[]
}

interface EchoInspection {
    inspectionType: string
    timeInSeconds: number
}

export interface EchoPlantInfo {
    plantCode: string
    projectDescription: string
}
