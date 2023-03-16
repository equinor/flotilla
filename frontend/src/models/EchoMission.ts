export interface EchoMission {
    id: number
    name: string
    url: string
    tags: EchoTag[]
}

export interface EchoTag {
    id: number
    tagId: string
    url: string
    inspections: EchoInspection[]
}

export interface EchoInspection {
    inspectionType: string
    timeInSeconds: number
}

export interface EchoPlantInfo {
    installationCode: string
    projectDescription: string
}
