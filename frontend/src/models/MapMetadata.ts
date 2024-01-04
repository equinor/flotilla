export interface MapMetadata {
    mapName: string
    boundary: Boundary
    transformationMatrices: TransformationMatrices
}

interface Boundary {
    x1: number
    x2: number
    y1: number
    y2: number
}

interface TransformationMatrices {
    c1: number
    c2: number
    d1: number
    d2: number
}
