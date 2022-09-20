export interface EchoMission {
    id: number
    name: string
    url: string
    tags: Tag[]
}

export interface Tag {
    id: number
    tagId: string
    url: string
    inspections: Inspection[]
}

export interface Inspection{
    inspectionType: string
    timeInSeconds: number
}