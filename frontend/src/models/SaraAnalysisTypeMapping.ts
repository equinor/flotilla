import { AnalysisType } from './MissionDefinition'

const saraAnalysisTypeStringToEnum: Record<string, AnalysisType> = {
    cloe: AnalysisType.CLOE,
    fencilla: AnalysisType.Fencilla,
    'thermal-reading': AnalysisType.ThermalReading,
    CO2: AnalysisType.CO2,
}

export const saraAnalysisTypeToEnum = (analysisType?: string): AnalysisType | undefined =>
    analysisType !== undefined ? saraAnalysisTypeStringToEnum[analysisType] : undefined
