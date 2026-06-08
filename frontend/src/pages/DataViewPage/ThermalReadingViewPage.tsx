import { useContext } from 'react'
import { Navigate } from 'react-router-dom'
import { InstallationContext } from 'components/Contexts/InstallationContext'
import { AnalysisTypes, DataView } from './DataView'

const THERMAL_READING_INSTALLATION_CODE = 'KAA'

export const ThermalReadingViewPage = () => {
    const { installation } = useContext(InstallationContext)

    if (installation.installationCode?.toUpperCase() !== THERMAL_READING_INSTALLATION_CODE) {
        return <Navigate to="/not-found" replace />
    }

    return (
        <DataView
            analysisType={AnalysisTypes.ThermalReading}
            pageTitle="Data View for Thermal Reading for Pumps"
            plotTitle="Estimated maximum temperature"
            plotAriaLabel="Estimated maximum temperature"
            plotYLabel="Temperature [°C]"
            plotYMin={-20}
            plotYMax={100}
            latestValueUnit="°C"
        />
    )
}
