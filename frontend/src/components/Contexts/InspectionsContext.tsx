import { createContext, FC, useContext, useEffect } from 'react'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'
import { SaraAnalysisResultReady, SaraInspectionVisualizationReady } from 'models/Inspection'
import { useQuery } from '@tanstack/react-query'
import { queryClient } from '../../App'
import { useBackendApi } from 'api/UseBackendApi'

interface IInspectionsContext {
    fetchImageData: (inspectionId: string) => any
    fetchAnalysisData: (inspectionId: string) => any
    fetchValueData: (inspectionId: string) => any
}

interface Props {
    children: React.ReactNode
}

const defaultInspectionsContext = {
    fetchImageData: () => undefined,
    fetchAnalysisData: () => undefined,
    fetchValueData: () => undefined,
}

const InspectionsContext = createContext<IInspectionsContext>(defaultInspectionsContext)

export const InspectionsProvider: FC<Props> = ({ children }) => {
    const { registerEvent, connectionReady } = useSignalRContext()
    const backendApi = useBackendApi()

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

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.analysisResultReady, (username: string, message: string) => {
                const analysisResultData: SaraAnalysisResultReady = JSON.parse(message)
                queryClient.invalidateQueries({
                    queryKey: ['fetchAnalysisData', analysisResultData.inspectionId],
                })
                fetchAnalysisData(analysisResultData.inspectionId)
            })
        }
    }, [registerEvent, connectionReady])

    const fetchImageData = (
        inspectionId: string
    ): { data: string | undefined; isPending: boolean; isError: boolean } => {
        const result = useQuery({
            queryKey: ['fetchInspectionData', inspectionId],
            queryFn: async () => {
                const imageBlob = await backendApi.getInspection(inspectionId)
                return URL.createObjectURL(imageBlob)
            },
            retry: 1,
            staleTime: 10 * 60 * 1000, // If data is received, stale time is 10 min before making new API call
            enabled: inspectionId !== undefined,
        })

        return { data: result.data, isPending: result.isPending, isError: result.isError }
    }

    const fetchAnalysisData = (
        inspectionId: string
    ): { data: string | undefined; isPending: boolean; isError: boolean } => {
        const result = useQuery({
            queryKey: ['fetchAnalysisData', inspectionId],
            queryFn: async () => {
                const imageBlob = await backendApi.getAnalysis(inspectionId)
                return URL.createObjectURL(imageBlob)
            },
            retry: 1,
            staleTime: 10 * 60 * 1000, // If data is received, stale time is 10 min before making new API call
            enabled: inspectionId !== undefined,
        })
        return { data: result.data, isPending: result.isPending, isError: result.isError }
    }

    const fetchValueData = (
        inspectionId: string
    ): { data: number | undefined; isPending: boolean; isError: boolean } => {
        const result = useQuery({
            queryKey: ['fetchValueData', inspectionId],
            queryFn: async () => {
                const value = await backendApi.getValue(inspectionId)
                return value
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
                fetchImageData,
                fetchAnalysisData,
                fetchValueData,
            }}
        >
            {children}
        </InspectionsContext.Provider>
    )
}

export const useInspectionsContext = () => useContext(InspectionsContext)
