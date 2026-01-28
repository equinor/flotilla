export interface InspectionArea {
    id: string
    inspectionAreaName: string
    plantName: string
    plantCode: string
    installationCode: string
    areaPolygon?: AreaPolygon
}

interface AreaPolygon {
    zmin: number
    zmax: number
    positions: PolygonPoint[]
}

export interface PolygonPoint {
    x: number
    y: number
}
