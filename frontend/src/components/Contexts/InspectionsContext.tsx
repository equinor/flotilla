import { createContext, FC, useContext, useEffect, useRef } from 'react'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'
import { SaraAnalysisResultReady, SaraInspectionVisualizationReady } from 'models/Inspection'
import { useQuery } from '@tanstack/react-query'
import { queryClient } from '../../App'
import { useBackendApi } from 'api/UseBackendApi'

interface IImageData {
    data: string | undefined
    isPending: boolean
    isError: boolean
}
interface IValueData {
    data: number | undefined
    isPending: boolean
    isError: boolean
}
interface IInspectionsContext {
    fetchImageData: (inspectionId: string) => IImageData
    fetchAnalysisData: (inspectionId: string) => IImageData
    fetchValueData: (inspectionId: string) => IValueData
}

interface Props {
    children: React.ReactNode
}

const defaultInspectionsContext = {
    fetchImageData: () => ({ data: undefined, isPending: false, isError: true }),
    fetchAnalysisData: () => ({ data: undefined, isPending: false, isError: true }),
    fetchValueData: () => ({ data: undefined, isPending: false, isError: true }),
}

const InspectionsContext = createContext<IInspectionsContext>(defaultInspectionsContext)

export const InspectionsProvider: FC<Props> = ({ children }) => {
    const { registerEvent, connectionReady } = useSignalRContext()
    const backendApi = useBackendApi()

    // Keep a stable ref to backendApi so callbacks don't capture a stale closure
    const backendApiRef = useRef(backendApi)
    useEffect(() => {
        backendApiRef.current = backendApi
    }, [backendApi])

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.inspectionVisualizationReady, (username: string, message: string) => {
                const inspectionVisualizationData: SaraInspectionVisualizationReady = JSON.parse(message)
                const inspectionId = inspectionVisualizationData.inspectionId
                queryClient.invalidateQueries({
                    queryKey: ['fetchInspectionData', inspectionId],
                })
                queryClient.fetchQuery({
                    queryKey: ['fetchInspectionData', inspectionId],
                    queryFn: async () => {
                        const imageBlob = await backendApiRef.current.getInspection(inspectionId)
                        return URL.createObjectURL(imageBlob)
                    },
                    retry: 1,
                    staleTime: 10 * 60 * 1000,
                })
            })
        }
    }, [registerEvent, connectionReady])

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.analysisResultReady, (username: string, message: string) => {
                const analysisResultData: SaraAnalysisResultReady = JSON.parse(message)
                const inspectionId = analysisResultData.inspectionId
                queryClient.invalidateQueries({
                    queryKey: ['fetchAnalysisData', inspectionId],
                })
                queryClient.fetchQuery({
                    queryKey: ['fetchAnalysisData', inspectionId],
                    queryFn: async () => {
                        const imageBlob = await backendApiRef.current.getAnalysis(inspectionId)
                        return URL.createObjectURL(imageBlob)
                    },
                    retry: 1,
                    staleTime: 10 * 60 * 1000,
                })
            })
        }
    }, [registerEvent, connectionReady])

    const fetchImageData = (inspectionId: string): IImageData => {
        // eslint-disable-next-line react-hooks/rules-of-hooks -- pre-existing design issue, tracked in #2698
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

    const fetchAnalysisData = (inspectionId: string): IImageData => {
        // eslint-disable-next-line react-hooks/rules-of-hooks -- pre-existing design issue, tracked in #2698
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

    const fetchValueData = (inspectionId: string): IValueData => {
        // eslint-disable-next-line react-hooks/rules-of-hooks -- pre-existing design issue, tracked in #2698
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
