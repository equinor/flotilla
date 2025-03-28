import { createContext, FC, useContext, useEffect, useState } from 'react'
import { Task } from 'models/Task'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'
import { SaraInspectionVisualizationReady } from 'models/Inspection'
import { useQuery } from '@tanstack/react-query'
import { BackendAPICaller } from 'api/ApiCaller'
import { queryClient } from '../../App'

interface IInspectionsContext {
    selectedInspectionTask: Task | undefined
    switchSelectedInspectionTask: (selectedInspectionTask: Task | undefined) => void
    fetchImageData: (inspectionId: string) => any
}

interface Props {
    children: React.ReactNode
}

const defaultInspectionsContext = {
    selectedInspectionTask: undefined,
    switchSelectedInspectionTask: () => undefined,
    fetchImageData: () => undefined,
}

const InspectionsContext = createContext<IInspectionsContext>(defaultInspectionsContext)

export const InspectionsProvider: FC<Props> = ({ children }) => {
    const { registerEvent, connectionReady } = useSignalRContext()
    const [selectedInspectionTask, setSelectedInspectionTask] = useState<Task>()

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.inspectionVisualizationReady, (username: string, message: string) => {
                const inspectionVisualizationData: SaraInspectionVisualizationReady = JSON.parse(message)
                queryClient.invalidateQueries({
                    queryKey: ['fetchInspectionData', inspectionVisualizationData.inspectionId],
                })
                fetchImageData(inspectionVisualizationData.inspectionId)
            })
        }
    }, [registerEvent, connectionReady])

    const switchSelectedInspectionTask = (selectedTask: Task | undefined) => {
        setSelectedInspectionTask(selectedTask)
    }

    const fetchImageData = (
        inspectionId: string
    ): { data: string | undefined; isPending: boolean; isError: boolean } => {
        const result = useQuery({
            queryKey: ['fetchInspectionData', inspectionId],
            queryFn: async () => {
                const imageBlob = await BackendAPICaller.getInspection(inspectionId)
                return URL.createObjectURL(imageBlob)
            },
            retry: 1,
            staleTime: 10 * 60 * 1000, // If data is received, stale time is 10 min before making new API call
            enabled: inspectionId !== undefined,
        })

        return { data: result.data, isPending: result.isPending, isError: result.isError }
    }

    return (
        <InspectionsContext.Provider
            value={{
                selectedInspectionTask,
                switchSelectedInspectionTask,
                fetchImageData,
            }}
        >
            {children}
        </InspectionsContext.Provider>
    )
}

export const useInspectionsContext = () => useContext(InspectionsContext)
