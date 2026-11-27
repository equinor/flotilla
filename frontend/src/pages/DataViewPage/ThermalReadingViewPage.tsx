import { useContext } from 'react'
import { Navigate } from 'react-router'
import { InstallationContext } from 'contexts/InstallationContext'
import { DataView } from './DataView'
import { DataViewName } from 'models/Installation'
import { AnalysisType } from 'models/MissionDefinition'

export const ThermalReadingViewPage = () => {
    const { installation } = useContext(InstallationContext)

    if (installation.relevantDataViews?.includes(DataViewName.THERMAL_READING) === false) {
        return <Navigate to="/not-found" replace />
    }

    return (
        <DataView
            analysisType={AnalysisType.ThermalReading}
            pageTitle="Data View for Thermal Reading for Pumps"
            plotTitle="Estimated maximum temperature"
            plotAriaLabel="Estimated maximum temperature"
            plotYLabel="Temperature [°C]"
            plotYMin={-20}
            plotYMax={100}
        />
    )
}
