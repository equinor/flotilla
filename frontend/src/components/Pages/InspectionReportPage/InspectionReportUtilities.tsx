import { useQuery } from '@tanstack/react-query'
import { BackendAPICaller } from 'api/ApiCaller'
import { Task, TaskStatus } from 'models/Task'
import { StyledInspectionImage } from './InspectionStyles'

export const fetchImageData = (task: Task) => {
    const data = useQuery({
        queryKey: ['fetchInspectionData', task.isarTaskId],
        queryFn: async () => {
            const imageBlob = await BackendAPICaller.getInspection(task.inspection.isarInspectionId)
            return URL.createObjectURL(imageBlob)
        },
        retryDelay: 60 * 1000, // Waits 1 min before retrying, regardless of how many retries
        staleTime: 10 * 60 * 1000, // If data is received, stale time is 10 min before making new API call
        enabled:
            task.status === TaskStatus.Successful &&
            task.isarTaskId !== undefined &&
            task.inspection.isarInspectionId !== undefined,
    })
    return data
}

export const GetInspectionImage = ({ task }: { task: Task }) => {
    const { data } = fetchImageData(task)
    return <>{data !== undefined && <StyledInspectionImage src={data} />}</>
}
