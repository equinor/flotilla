import { Task } from 'models/Task'
import { AnalysisTypes, DataView } from './DataView'

enum CloeAnalysableDescriptions {
    SphericalGlass = 'Spherical glass',
}

const checkIfAnalysableDescription = (description: string) => {
    return Object.values(CloeAnalysableDescriptions).includes(description as CloeAnalysableDescriptions)
}

export const CloeDataViewPage = () => (
    <DataView
        analysisType={AnalysisTypes.CLOE}
        taskFilter={(task: Task) => (task.description ? checkIfAnalysableDescription(task.description) : false)}
        pageTitle="Data View for Constant Level Oilers"
        plotTitle="Estimated oil level"
        plotAriaLabel="Estimated oil level"
        plotYLabel="Fill [%]"
        plotYMin={0}
        plotYMax={100}
    />
)
