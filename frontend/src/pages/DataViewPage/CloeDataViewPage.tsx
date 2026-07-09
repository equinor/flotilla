import { DataView } from './DataView'
import { AnalysisType } from 'models/MissionDefinition'

export const CloeDataViewPage = () => (
    <DataView
        analysisType={AnalysisType.CLOE}
        pageTitle="Data View for Constant Level Oilers"
        plotTitle="Estimated oil level"
        plotAriaLabel="Estimated oil level"
        plotYLabel="Fill [%]"
        plotYMin={0}
        plotYMax={100}
    />
)
