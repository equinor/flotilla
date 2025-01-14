import { useInspectionsContext } from 'components/Contexts/InpectionsContext'
import { Task } from 'models/Task'
import { StyledInspectionImage } from './InspectionStyles'

export const GetInspectionImage = ({ task }: { task: Task }) => {
    const { fetchImageData } = useInspectionsContext()
    const { data } = fetchImageData(task.inspection.isarInspectionId)
    return <>{data !== undefined && <StyledInspectionImage src={data} />}</>
}
